

using BankingApp.Application.Features.Account.Queries.GetById;
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
    public class GetAccountByIdQueryHandlerTests
    {
        private readonly Mock<ILogger<GetAccountByIdQueryHandler>> _loggerMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Account>> _accountRepoMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly GetAccountByIdQueryHandler _handler;

        public GetAccountByIdQueryHandlerTests()
        {
            _loggerMock = new Mock<ILogger<GetAccountByIdQueryHandler>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _accountRepoMock = new Mock<IGenericRepository<Account>>();
            _cacheServiceMock = new Mock<ICacheService>();

            _unitOfWorkMock.Setup(u => u.GetRepository<Account>())
                .Returns(_accountRepoMock.Object);

            _handler = new GetAccountByIdQueryHandler(
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _cacheServiceMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldReturnFromCache_WhenAccountIsCached()
        {
            var query = new GetAccountByIdQuery { AccountId = 1 };
            var cachedAccount = new Account { Id = 1, AccountNumber = 12345, Balance = 500 };

            _cacheServiceMock
                .Setup(c => c.RetrieveFromCacheAsync<Account>("Account_1"))
                .ReturnsAsync(cachedAccount);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Equal(ResponseCode.SUCCESSFUL, result.StatusCode);
            Assert.Equal("Account retrieved from cache.", result.StatusMessage);
            Assert.Equal(cachedAccount, result.Data);

            _accountRepoMock.Verify(r => r.Get(
                It.IsAny<Expression<Func<Account, bool>>>(),
                It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFromDb_WhenNotCached()
        {
            var query = new GetAccountByIdQuery { AccountId = 2 };
            var dbAccount = new Account { Id = 2, AccountNumber = 22222, Balance = 1000 };

            _cacheServiceMock
                .Setup(c => c.RetrieveFromCacheAsync<Account>("Account_2"))
                .ReturnsAsync((Account)null);

            _accountRepoMock
                .Setup(r => r.Get(
                    It.IsAny<Expression<Func<Account, bool>>>(),
                    It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                    It.IsAny<string>()
                ))
                .Returns(new List<Account> { dbAccount }.AsQueryable());

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Equal(ResponseCode.SUCCESSFUL, result.StatusCode);
            Assert.Equal("Account retrieved successfully.", result.StatusMessage);
            Assert.Equal(dbAccount, result.Data);

            _cacheServiceMock.Verify(c => c.CacheAbsoluteObject("Account_2", dbAccount, 10), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenAccountIdInvalid()
        {
            var query = new GetAccountByIdQuery { AccountId = 0 }; // Invalid ID

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Equal(ResponseCode.BadRequest, result.StatusCode);
            Assert.Contains("Account ID must be greater than zero", result.StatusMessage);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenAccountNotFoundInDb()
        {
            var query = new GetAccountByIdQuery { AccountId = 5 };

            _cacheServiceMock
                .Setup(c => c.RetrieveFromCacheAsync<Account>("Account_5"))
                .ReturnsAsync((Account)null);

            _accountRepoMock
               .Setup(r => r.Get(
                   It.IsAny<Expression<Func<Account, bool>>>(),
                   It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                   It.IsAny<string>()
               ))
               .Returns(new List<Account>().AsQueryable());

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Equal(ResponseCode.BadRequest, result.StatusCode);
            Assert.Contains("Account not found", result.StatusMessage);
        }
    }
}
