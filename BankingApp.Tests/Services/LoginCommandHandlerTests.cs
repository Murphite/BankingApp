using BankingApp.Application.Exceptions;
using BankingApp.Application.Features.Auth.Commands.Login;
using BankingApp.Domain.Models;
using BankingApp.Domain.Utility.Interfaces;
using BankingApp.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BankingApp.Tests.Services
{
    public class LoginCommandHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock;
        private readonly BankingDbContext _dbContext;
        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            _userManagerMock = MockUserManager();
            _jwtServiceMock = new Mock<IJwtService>();
            _loggerMock = new Mock<ILogger<LoginCommandHandler>>();

            // Using in-memory DbContext
           
            _handler = new LoginCommandHandler(
                _userManagerMock.Object,
                _jwtServiceMock.Object,
                _loggerMock.Object,
                _dbContext
            );
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenCredentialsValid()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Email = "test@example.com",
                UserName = "test@example.com",
                IsActive = true,
                UserType = "Customer",
                CustomerDetails = new Customer { Id = 1, Address = "123 St", Age = 30 }
            };

            _userManagerMock.Setup(x => x.Users)
                .Returns(new List<ApplicationUser> { user }.AsQueryable());

            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "Password123"))
                .ReturnsAsync(true);

            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Customer" });

            _jwtServiceMock.Setup(x => x.GenerateToken(user, It.IsAny<IList<string>>()))
                .Returns("fake-jwt-token");

            var command = new LoginCommand("test@example.com", "Password123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("fake-jwt-token", result.Token);
            Assert.Contains("Customer", result.Roles);
            _jwtServiceMock.Verify(x => x.GenerateToken(user, It.IsAny<IList<string>>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnInvalidCredentials_WhenUserNotFound()
        {
            // Arrange
            _userManagerMock.Setup(x => x.Users)
                .Returns(new List<ApplicationUser>().AsQueryable());

            var command = new LoginCommand("notfound@example.com", "Password123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid credentials", result.ErrorMessage);
        }

        [Fact]
        public async Task Handle_ShouldReturnAccountInactive_WhenUserInactive()
        {
            var user = new ApplicationUser
            {
                Email = "inactive@example.com",
                UserName = "inactive@example.com",
                IsActive = false
            };

            _userManagerMock.Setup(x => x.Users)
                .Returns(new List<ApplicationUser> { user }.AsQueryable());

            var command = new LoginCommand("inactive@example.com", "Password123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Account inactive", result.ErrorMessage);
        }

        [Fact]
        public async Task Handle_ShouldReturnInvalidCredentials_WhenPasswordIncorrect()
        {
            var user = new ApplicationUser
            {
                Email = "wrongpass@example.com",
                UserName = "wrongpass@example.com",
                IsActive = true
            };

            _userManagerMock.Setup(x => x.Users)
                .Returns(new List<ApplicationUser> { user }.AsQueryable());

            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "WrongPass"))
                .ReturnsAsync(false);

            var command = new LoginCommand("wrongpass@example.com", "WrongPass");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid credentials", result.ErrorMessage);
        }

        [Fact]
        public async Task Handle_ShouldThrowValidationException_WhenEmailInvalid()
        {
            var command = new LoginCommand("", "Password123");

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        // Helper: Mock UserManager
        private static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null
            );
        }
    }
}