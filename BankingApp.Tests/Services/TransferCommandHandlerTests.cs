

using BankingApp.Application.Features.Account.Command.Transfer;
using BankingApp.Domain.Constants;
using BankingApp.Domain.Models;
using BankingApp.Domain.Utility.Interfaces;
using BankingApp.Infrastructure.Persistence.Repositories;
using BankingApp.Infrastructure.Persistence.UnitOfWork;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace BankingApp.Tests.Services
{
    public class TransferCommandHandlerTests
    {
        private readonly Mock<ILogger<TransferCommandHandler>> _loggerMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Account>> _accountRepoMock;
        private readonly Mock<IGenericRepository<Transaction>> _transactionRepoMock;
        private readonly Mock<IPaymentGateway> _paymentGatewayMock;
        private readonly TransferCommandHandler _handler;

        public TransferCommandHandlerTests()
        {
            _loggerMock = new Mock<ILogger<TransferCommandHandler>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _accountRepoMock = new Mock<IGenericRepository<Account>>();
            _transactionRepoMock = new Mock<IGenericRepository<Transaction>>();
            _paymentGatewayMock = new Mock<IPaymentGateway>();

            _unitOfWorkMock.Setup(u => u.GetRepository<Account>())
                .Returns(_accountRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Transaction>())
                .Returns(_transactionRepoMock.Object);


            _handler = new TransferCommandHandler(
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _paymentGatewayMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldTransfer_WhenValidRequest()
        {
            var command = new TransferCommand
            {
                FromAccountId = 1,
                ToAccountId = 2,
                Amount = 200,
                Description = "Payment"
            };

            _paymentGatewayMock
                .Setup(pg => pg.InitiateTransferAsync(command.FromAccountId, command.ToAccountId, command.Amount))
                .ReturnsAsync(new PaymentResult { Success = true, Reference = "REF_T123" });

            _accountRepoMock
                .Setup(r => r.Get(
                    It.IsAny<Expression<Func<Account, bool>>>(),
                    It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                    It.IsAny<string>()
                ))
                .Returns((Expression<Func<Account, bool>> predicate,
                          Func<IQueryable<Account>, IOrderedQueryable<Account>> orderBy,
                          string includeProperties) =>
                {
                    var list = new List<Account>
                    {
                        new Account { Id = 1, AccountNumber = 111, Balance = 1000, IsDeleted = false },
                        new Account { Id = 2, AccountNumber = 222, Balance = 500, IsDeleted = false }
                    };
                    return list.Where(predicate.Compile()).AsQueryable();
                });

            _unitOfWorkMock.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(ResponseCode.SUCCESSFUL, result.StatusCode);
            _transactionRepoMock.Verify(r => r.Insert(It.IsAny<Transaction>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenPaymentGatewayFails()
        {
            var command = new TransferCommand
            {
                FromAccountId = 1,
                ToAccountId = 2,
                Amount = 200
            };

            _paymentGatewayMock
                .Setup(pg => pg.InitiateTransferAsync(command.FromAccountId, command.ToAccountId, command.Amount))
                .ReturnsAsync(new PaymentResult { Success = false, Message = "Gateway error" });

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(ResponseCode.BadRequest, result.StatusCode);
            Assert.Equal("Gateway error", result.StatusMessage);
            _transactionRepoMock.Verify(r => r.Insert(It.IsAny<Transaction>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenInsufficientFunds()
        {
            var command = new TransferCommand
            {
                FromAccountId = 1,
                ToAccountId = 2,
                Amount = 2000
            };

            _paymentGatewayMock
                .Setup(pg => pg.InitiateTransferAsync(command.FromAccountId, command.ToAccountId, command.Amount))
                .ReturnsAsync(new PaymentResult { Success = true, Reference = "REF_T123" });

            _accountRepoMock
                .Setup(r => r.Get(
                    It.IsAny<Expression<Func<Account, bool>>>(),
                    It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                    It.IsAny<string>()
                ))
                .Returns((Expression<Func<Account, bool>> predicate,
                          Func<IQueryable<Account>, IOrderedQueryable<Account>> orderBy,
                          string includeProperties) =>
                {
                    var list = new List<Account>
                    {
                        new Account { Id = 1, AccountNumber = 111, Balance = 100, IsDeleted = false },
                        new Account { Id = 2, AccountNumber = 222, Balance = 500, IsDeleted = false }
                    };
                    return list.Where(predicate.Compile()).AsQueryable();
                });

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(ResponseCode.BadRequest, result.StatusCode);
            Assert.Contains("Insufficient funds", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
            _transactionRepoMock.Verify(t => t.RollBack(), Times.Once);
        }
    }
}
