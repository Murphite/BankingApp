
using BankingApp.Application.Exceptions;
using BankingApp.Application.Features.Auth.Commands.Register;
using BankingApp.Domain.Models;
using BankingApp.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BankingApp.Tests.Services
{
    public class RegisterCommandHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<BankingDbContext> _dbContextMock;
        private readonly Mock<ILogger<RegisterCommandHandler>> _loggerMock;
        private readonly RegisterCommandHandler _handler;
        private readonly List<Customer> _customerStore = new();
        private readonly List<Admin> _adminStore = new();

        public RegisterCommandHandlerTests()
        {
            // Mock UserManager
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);
           
            _dbContextMock.Object.Customers = CreateMockDbSet(_customerStore).Object;
            _dbContextMock.Object.Admins = CreateMockDbSet(_adminStore).Object;

            _loggerMock = new Mock<ILogger<RegisterCommandHandler>>();

            _handler = new RegisterCommandHandler(
                _userManagerMock.Object,
                _dbContextMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldRegisterCustomer_WhenValid()
        {
            var cmd = new RegisterCommand(
                Email: "test@example.com",
                Password: "Pass@123",
                FullName: "Test User",
                UserType: "Customer",
                Address: "123 Main St",
                DateOfBirth: new DateTime(1990, 1, 1),
                PhoneNumber: "08123456789"

            );

            _userManagerMock
                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), cmd.Password))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock
                .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Single(_dbContextMock.Object.Customers);
            _userManagerMock.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), cmd.Password), Times.Once);
            _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenValidationFails()
        {
            var cmd = new RegisterCommand(
                Email: "bademail",
                Password: "123",
                FullName: "",
                UserType: "Customer",
                PhoneNumber: "08123456789"
            );

            await Assert.ThrowsAsync<ValidationException>(() =>
                _handler.Handle(cmd, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenUserCreationFails()
        {
            var cmd = new RegisterCommand(
                Email: "test@example.com",
                Password: "Pass@123",
                FullName: "Test User",
                UserType: "Customer",
                PhoneNumber: "08123456789",
                Address: "123 Main St",
                DateOfBirth: new DateTime(1990, 1, 1)
            );

            _userManagerMock
                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), cmd.Password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Duplicate email" }));

            var result = await _handler.Handle(cmd, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("Duplicate email", result.Errors);
            _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        // helper for creating mock DbSet
        private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var dbSet = new Mock<DbSet<T>>();
            dbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            dbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            dbSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>(sourceList.Add);
            return dbSet;
        }
    }
}
