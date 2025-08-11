

using BankingApp.Application.Features.Account.Command.Deposit;
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
    public class DepositCommandHandlerTests
    {
        private readonly Mock<ILogger<DepositCommandHandler>> _loggerMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Account>> _accountRepoMock;
        private readonly Mock<IGenericRepository<Transaction>> _transactionRepoMock;
        private readonly Mock<IPaymentGateway> _paymentGatewayMock;
        private readonly DepositCommandHandler _handler;

        public DepositCommandHandlerTests()
        {
            _loggerMock = new Mock<ILogger<DepositCommandHandler>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _accountRepoMock = new Mock<IGenericRepository<Account>>();
            _transactionRepoMock = new Mock<IGenericRepository<Transaction>>();
            _paymentGatewayMock = new Mock<IPaymentGateway>();

            _unitOfWorkMock.Setup(u => u.GetRepository<Account>())
                .Returns(_accountRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Transaction>())
                .Returns(_transactionRepoMock.Object);

            _handler = new DepositCommandHandler(
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _paymentGatewayMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldDeposit_WhenValidRequest()
        {
            // Arrange
            var command = new DepositCommand
            {
                AccountId = 123,
                Amount = 500
            };

            _paymentGatewayMock
                .Setup(pg => pg.InitiateDepositAsync(command.AccountId, command.Amount))
                .ReturnsAsync(new PaymentResult { Success = true, Reference = "REF123" });

            _accountRepoMock
                .Setup(r => r.Get(
                    It.IsAny<Expression<Func<Account, bool>>>(),
                    It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                    It.IsAny<string>()
                ))
                .Returns(new[] { new Account { Id = 1, AccountNumber = 123, Balance = 1000, IsDeleted = false } }.AsQueryable());

            _unitOfWorkMock.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            _transactionRepoMock.Verify(r => r.Insert(It.IsAny<Transaction>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenPaymentGatewayFails()
        {
            // Arrange
            var command = new DepositCommand
            {
                AccountId = 123,
                Amount = 500
            };

            _paymentGatewayMock
                .Setup(pg => pg.InitiateDepositAsync(command.AccountId, command.Amount))
                .ReturnsAsync(new PaymentResult { Success = false, Message = "Insufficient funds" });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
            _transactionRepoMock.Verify(r => r.Insert(It.IsAny<Transaction>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenAccountNotFound()
        {
            // Arrange
            var command = new DepositCommand
            {
                AccountId = 123,
                Amount = 500
            };

            _paymentGatewayMock
                .Setup(pg => pg.InitiateDepositAsync(command.AccountId, command.Amount))
                .ReturnsAsync(new PaymentResult { Success = true, Reference = "REF123" });

            _accountRepoMock
               .Setup(r => r.Get(
                   It.IsAny<Expression<Func<Account, bool>>>(),
                   It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                   It.IsAny<string>()
               ))
               .Returns(new List<Account>().AsQueryable());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
            _transactionRepoMock.Verify(r => r.Insert(It.IsAny<Transaction>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
