
using BankingApp.Application.Exceptions;
using BankingApp.Application.Features.Auth.Commands.Register;
using BankingApp.Domain.Models;
using BankingApp.Infrastructure.Persistence.Context;
using BankingApp.Infrastructure.Persistence.Repositories;
using BankingApp.Infrastructure.Persistence.UnitOfWork;
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
        private readonly Mock<IUnitOfWork<BankingDbContext>> _unitOfWorkMock;
        private readonly Mock<ILogger<RegisterCommandHandler>> _loggerMock;

        private readonly List<Customer> _customerStore;
        private readonly List<Admin> _adminStore;

        private RegisterCommandHandler _handler;

        public RegisterCommandHandlerTests()
        {
            // Mock UserManager
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null
            );

            _unitOfWorkMock = new Mock<IUnitOfWork<BankingDbContext>>();
            _loggerMock = new Mock<ILogger<RegisterCommandHandler>>();

            _customerStore = new List<Customer>();
            _adminStore = new List<Admin>();

            // Mock repository for Customer
            var customerRepoMock = new Mock<IGenericRepository<Customer>>();
            customerRepoMock.Setup(r => r.Insert(It.IsAny<Customer>()))
                .Callback<Customer>(_customerStore.Add);

            // Mock repository for Admin
            var adminRepoMock = new Mock<IGenericRepository<Admin>>();
            adminRepoMock.Setup(r => r.Insert(It.IsAny<Admin>()))
                .Callback<Admin>(_adminStore.Add);

            // UnitOfWork should return these repos
            _unitOfWorkMock.Setup(u => u.GetRepository<Customer>())
                .Returns(customerRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Admin>())
                .Returns(adminRepoMock.Object);

            // Mock SaveAsync
            _unitOfWorkMock.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Mock BeginTransaction (no-op)
            _unitOfWorkMock.Setup(u => u.BeginTransaction())
                .Returns(Mock.Of<IDatabaseTransaction>());

            _handler = new RegisterCommandHandler(
                _userManagerMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldRegisterCustomer_WhenRoleIsCustomer()
        {
            // Arrange
            var command = new RegisterCommand(
                Email: "test@example.com",
                Password: "Password123!",
                FullName: "Test User",
                UserType: "Customer",
                PhoneNumber: "09087654321",
                Address: "123 Main St",
                DateOfBirth: new DateTime(1990, 1, 1),
                Department: null
            );
            _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Single(_customerStore); // Customer was added
            Assert.Empty(_adminStore);     // No admin was added
        }

        [Fact]
        public async Task Handle_ShouldRegisterAdmin_WhenRoleIsAdmin()
        {
            // Arrange
            var command = new RegisterCommand(
                Email: "admin@example.com",
                Password: "Admin123!",
                FullName: "Test Admin",
                UserType: "Admin",
                PhoneNumber: "09087654321",
                Address: null,
                DateOfBirth: null,
                Department: null
            );

            _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Single(_adminStore);   // Admin was added
            Assert.Empty(_customerStore); // No customer was added
        }
    }

}
