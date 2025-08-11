

using BankingApp.Application.Features.Transaction.Queries.MonthlyStatement;
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
    public class GetMonthlyStatementQueryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Transaction>> _transactionRepoMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly Mock<ILogger<GetMonthlyStatementQueryHandler>> _loggerMock;
        private readonly GetMonthlyStatementQueryHandler _handler;

        public GetMonthlyStatementQueryHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _transactionRepoMock = new Mock<IGenericRepository<Transaction>>();
            _cacheServiceMock = new Mock<ICacheService>();
            _loggerMock = new Mock<ILogger<GetMonthlyStatementQueryHandler>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<Transaction>())
                .Returns(_transactionRepoMock.Object);

            _handler = new GetMonthlyStatementQueryHandler(
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _cacheServiceMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldReturnDataFromCache_WhenCacheExists()
        {
            // Arrange
            var query = new GetMonthlyStatementQuery
            {
                AccountId = 1,
                Year = 2025,
                Month = 1
            };

            var cachedData = new List<Transaction> { new Transaction { Id = 1, AccountId = 1 } };
            _cacheServiceMock.Setup(c => c.RetrieveFromCacheAsync<List<Transaction>>(It.IsAny<string>()))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResponseCode.SUCCESSFUL, result.StatusCode);
            Assert.Equal(cachedData, result.Data);
            Assert.Contains("retrieved from cache", result.StatusMessage);

            _transactionRepoMock
               .Setup(r => r.Get(
                   It.IsAny<Expression<Func<Transaction, bool>>>(),
                   It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                   It.IsAny<string>()
               ))
               .Returns(new List<Transaction>().AsQueryable());
        }

        [Fact]
        public async Task Handle_ShouldFetchFromDbAndCache_WhenCacheIsEmpty()
        {
            // Arrange
            var query = new GetMonthlyStatementQuery
            {
                AccountId = 1,
                Year = 2025,
                Month = 1
            };

            _cacheServiceMock.Setup(c => c.RetrieveFromCacheAsync<List<Transaction>>(It.IsAny<string>()))
                .ReturnsAsync((List<Transaction>)null);

            var transactions = new List<Transaction>
        {
            new Transaction { Id = 1, AccountId = 1, TransactionDate = new DateTime(2025, 1, 15) }
        }.AsQueryable();

            _transactionRepoMock.Setup(r => r.Get(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<string>()
            )).Returns(transactions);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResponseCode.SUCCESSFUL, result.StatusCode);
            Assert.Single(result.Data);
            _cacheServiceMock.Verify(c => c.CacheAbsoluteObject(It.IsAny<string>(), It.IsAny<object>(), 10), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnBadRequest_WhenValidationFails()
        {
            // Arrange
            var query = new GetMonthlyStatementQuery
            {
                AccountId = 0, // invalid
                Year = 2025,
                Month = 1
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResponseCode.BadRequest, result.StatusCode);
            Assert.Contains("Account ID must be valid", result.StatusMessage);
        }

        [Fact]
        public async Task Handle_ShouldReturnBadRequest_OnUnexpectedError()
        {
            // Arrange
            var query = new GetMonthlyStatementQuery
            {
                AccountId = 1,
                Year = 2025,
                Month = 1
            };

            _cacheServiceMock.Setup(c => c.RetrieveFromCacheAsync<List<Transaction>>(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database failure"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResponseCode.BadRequest, result.StatusCode);
            Assert.Contains("Database failure", result.StatusMessage);
        }
    }
}
