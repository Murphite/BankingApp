

using BankingApp.Application.Features.Account.Command.Create;
using BankingApp.Domain.Constants;
using BankingApp.Domain.Models;
using BankingApp.Infrastructure.Persistence.Repositories;
using BankingApp.Infrastructure.Persistence.UnitOfWork;
using BankingApp.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace BankingApp.Tests.Services
{
    public class CreateAccountHandlerTests
    {
        private readonly Mock<ILogger<CreateAccountHandler>> _loggerMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Account>> _accountRepoMock;
        private readonly Mock<IGenericRepository<Transaction>> _transactionRepoMock;
        private readonly Mock<IDatabaseTransaction> _transactionMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly CreateAccountHandler _handler;

        public CreateAccountHandlerTests()
        {
            _loggerMock = new Mock<ILogger<CreateAccountHandler>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _accountRepoMock = new Mock<IGenericRepository<Account>>();
            _transactionRepoMock = new Mock<IGenericRepository<Transaction>>();
            _transactionMock = new Mock<IDatabaseTransaction>();
            _currentUserMock = new Mock<ICurrentUserService>();

            _unitOfWorkMock.Setup(u => u.GetRepository<Account>())
                .Returns(_accountRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Transaction>())
                .Returns(_transactionRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.BeginTransaction())
                .Returns(_transactionMock.Object);

            // Mock current user values
            _currentUserMock.Setup(c => c.UserId).Returns(Guid.NewGuid());
            _currentUserMock.Setup(c => c.FullName).Returns("Test User");

            _handler = new CreateAccountHandler(
                _loggerMock.Object,
                _unitOfWorkMock.Object,
                _currentUserMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldCreateAccount_WhenValidRequest()
        {
            var command = new CreateAccountCommand
            {
                AccountHolderName = "John Doe",
                InitialDeposit = 500
            };

            _accountRepoMock
                .Setup(r => r.Get(
                    It.IsAny<Expression<Func<Account, bool>>>(),
                    It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                    It.IsAny<string>()
                ))
                .Returns(new List<Account>().AsQueryable());

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(ResponseCode.SUCCESSFUL, result.StatusCode);
            Assert.Contains("Account created successfully", result.StatusMessage);

            _accountRepoMock.Verify(r => r.Insert(It.IsAny<Account>()), Times.Once);
            _transactionRepoMock.Verify(r => r.Insert(It.IsAny<Transaction>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.Save(), Times.Exactly(2));
            _transactionMock.Verify(t => t.Commit(), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldFailValidation_WhenDuplicateAccountHolder()
        {
            var command = new CreateAccountCommand
            {
                AccountHolderName = "Existing User",
                InitialDeposit = 100
            };

            _accountRepoMock
                .Setup(r => r.Get(
                    It.IsAny<Expression<Func<Account, bool>>>(),
                    It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                    It.IsAny<string>()
                ))
                .Returns(new List<Account>().AsQueryable());

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(ResponseCode.BadRequest, result.StatusCode);
            Assert.Contains("already exists", result.StatusMessage);

            _accountRepoMock.Verify(r => r.Insert(It.IsAny<Account>()), Times.Never);
            _transactionRepoMock.Verify(r => r.Insert(It.IsAny<Transaction>()), Times.Never);
            _transactionMock.Verify(t => t.Commit(), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldRollbackTransaction_OnUnexpectedError()
        {
            var command = new CreateAccountCommand
            {
                AccountHolderName = "John Doe",
                InitialDeposit = 200
            };

            _accountRepoMock
                .Setup(r => r.Get(
                    It.IsAny<Expression<Func<Account, bool>>>(),
                    It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                    It.IsAny<string>()
                ))
                .Returns(new List<Account>().AsQueryable());

            _accountRepoMock.Setup(r => r.Insert(It.IsAny<Account>()))
                .Throws(new Exception("Database insert failed"));

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(ResponseCode.BadRequest, result.StatusCode);
            Assert.Contains("Database insert failed", result.StatusMessage);

            _transactionMock.Verify(t => t.Rollback(), Times.Once);
        }
    }
}
