

using BankingApp.Application.Features.Account.Command.Withdraw;
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
    public class WithdrawCommandHandlerTests
    {
        private readonly Mock<ILogger<WithdrawCommandHandler>> _loggerMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Account>> _accountRepoMock;
        private readonly Mock<IGenericRepository<Transaction>> _transactionRepoMock;
        private readonly Mock<IPaymentGateway> _paymentGatewayMock;
        private readonly WithdrawCommandHandler _handler;

        public WithdrawCommandHandlerTests()
        {
            _loggerMock = new Mock<ILogger<WithdrawCommandHandler>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _accountRepoMock = new Mock<IGenericRepository<Account>>();
            _transactionRepoMock = new Mock<IGenericRepository<Transaction>>();
            _paymentGatewayMock = new Mock<IPaymentGateway>();

            _unitOfWorkMock.Setup(u => u.GetRepository<Account>())
                .Returns(_accountRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Transaction>())
                .Returns(_transactionRepoMock.Object);

            _handler = new WithdrawCommandHandler(
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _paymentGatewayMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldWithdraw_WhenValidRequest()
        {
            var command = new WithdrawCommand
            {
                AccountId = 123,
                Amount = 200
            };

            _paymentGatewayMock
                .Setup(pg => pg.InitiateWithdrawalAsync(command.AccountId, command.Amount))
                .ReturnsAsync(new PaymentResult { Success = true, Reference = "REFW123" });

            _accountRepoMock
                .Setup(r => r.Get(
                    It.IsAny<Expression<Func<Account, bool>>>(),
                    It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                    It.IsAny<string>()
                ))
                .Returns(new[] { new Account { Id = 1, AccountNumber = 123, Balance = 1000, IsDeleted = false } }.AsQueryable());

            _unitOfWorkMock.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result);
            _transactionRepoMock.Verify(r => r.Insert(It.IsAny<Transaction>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenPaymentGatewayFails()
        {
            var command = new WithdrawCommand
            {
                AccountId = 123,
                Amount = 200
            };

            _paymentGatewayMock
                .Setup(pg => pg.InitiateWithdrawalAsync(command.AccountId, command.Amount))
                .ReturnsAsync(new PaymentResult { Success = false, Message = "Insufficient funds" });

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result);
            _transactionRepoMock.Verify(r => r.Insert(It.IsAny<Transaction>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenAccountNotFound()
        {
            var command = new WithdrawCommand
            {
                AccountId = 123,
                Amount = 200
            };

            _paymentGatewayMock
                .Setup(pg => pg.InitiateWithdrawalAsync(command.AccountId, command.Amount))
                .ReturnsAsync(new PaymentResult { Success = true, Reference = "REFW123" });

            _accountRepoMock
               .Setup(r => r.Get(
                   It.IsAny<Expression<Func<Account, bool>>>(),
                   It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                   It.IsAny<string>()
               ))
               .Returns(new List<Account>().AsQueryable());

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result);
            _transactionRepoMock.Verify(r => r.Insert(It.IsAny<Transaction>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
