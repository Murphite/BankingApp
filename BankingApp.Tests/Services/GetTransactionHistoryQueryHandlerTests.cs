
using BankingApp.Application.Features.Transaction.Queries.GetTransactionHistory;
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
    public class GetTransactionHistoryQueryHandlerTests
    {
        private readonly Mock<ILogger<GetTransactionHistoryQueryHandler>> _loggerMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IGenericRepository<Transaction>> _transactionRepoMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly GetTransactionHistoryQueryHandler _handler;

        public GetTransactionHistoryQueryHandlerTests()
        {
            _loggerMock = new Mock<ILogger<GetTransactionHistoryQueryHandler>>();
            _uowMock = new Mock<IUnitOfWork>();
            _transactionRepoMock = new Mock<IGenericRepository<Transaction>>();
            _cacheServiceMock = new Mock<ICacheService>();

            _uowMock.Setup(u => u.GetRepository<Transaction>())
                .Returns(_transactionRepoMock.Object);

            _handler = new GetTransactionHistoryQueryHandler(
                _uowMock.Object,
                _loggerMock.Object,
                _cacheServiceMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldReturnTransactions_FromCache_WhenCacheExists()
        {
            // Arrange
            var accountId = 1;
            var cachedData = new List<Transaction> { new Transaction { AccountId = accountId, Amount = 100 } };

            _cacheServiceMock.Setup(c => c.RetrieveFromCacheAsync<List<Transaction>>($"Transactions_{accountId}"))
                .ReturnsAsync(cachedData);

            var query = new GetTransactionHistoryQuery { AccountId = accountId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResponseCode.SUCCESSFUL, result.StatusCode);
            Assert.Contains("from cache", result.StatusMessage);
            Assert.Equal(1, result.Data.Count);
            _transactionRepoMock
                .Setup(r => r.Get(
                    It.IsAny<Expression<Func<Transaction, bool>>>(),
                    It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                    It.IsAny<string>()
                ))
                .Returns(new List<Transaction>().AsQueryable());
        }

        [Fact]
        public async Task Handle_ShouldReturnTransactions_FromDatabase_WhenCacheMiss()
        {
            // Arrange
            var accountId = 2;
            var dbData = new List<Transaction>
        {
            new Transaction { AccountId = accountId, Amount = 200, TransactionDate = DateTime.UtcNow }
        };

            _cacheServiceMock.Setup(c => c.RetrieveFromCacheAsync<List<Transaction>>($"Transactions_{accountId}"))
                .ReturnsAsync((List<Transaction>)null);

            _transactionRepoMock
                .Setup(r => r.Get(
                    It.IsAny<Expression<Func<Transaction, bool>>>(),
                    It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                    It.IsAny<string>()
                ))
                .Returns(new List<Transaction>().AsQueryable());

            var query = new GetTransactionHistoryQuery { AccountId = accountId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResponseCode.SUCCESSFUL, result.StatusCode);
            Assert.Contains("retrieved successfully", result.StatusMessage);
            Assert.Single(result.Data);
            _cacheServiceMock.Verify(c => c.CacheAbsoluteObject($"Transactions_{accountId}", dbData, 10), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldFailValidation_WhenInvalidAccountId()
        {
            // Arrange
            var query = new GetTransactionHistoryQuery { AccountId = 0 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResponseCode.BadRequest, result.StatusCode);
            Assert.Contains("Account ID must be valid", result.StatusMessage);
            _transactionRepoMock
                .Setup(r => r.Get(
                    It.IsAny<Expression<Func<Transaction, bool>>>(),
                    It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                    It.IsAny<string>()
                ))
                .Returns(new List<Transaction>().AsQueryable());
        }

        [Fact]
        public async Task Handle_ShouldReturnBadRequest_OnException()
        {
            // Arrange
            var accountId = 3;

            _cacheServiceMock.Setup(c => c.RetrieveFromCacheAsync<List<Transaction>>($"Transactions_{accountId}"))
                .ThrowsAsync(new Exception("Unexpected failure"));

            var query = new GetTransactionHistoryQuery { AccountId = accountId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResponseCode.BadRequest, result.StatusCode);
            Assert.Contains("Unexpected failure", result.StatusMessage);
        }
    }
}
