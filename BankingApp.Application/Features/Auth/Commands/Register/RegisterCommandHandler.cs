using BankingApp.Domain.Models;
using BankingApp.Infrastructure.Persistence.Context;
using BankingApp.Infrastructure.Persistence.UnitOfWork;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BankingApp.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork<BankingDbContext> _unitOfWork;
        private readonly ILogger<RegisterCommandHandler> _logger;

        public RegisterCommandHandler(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork<BankingDbContext> unitOfWork,
            ILogger<RegisterCommandHandler> logger)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting registration for {Email}", request.Email);

            // ✅ Validation
            var validator = new RegisterCommandValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed: {Errors}", validationResult.Errors);
                throw new ValidationException(validationResult.Errors);
            }

            using var transaction = _unitOfWork.BeginTransaction();
            try
            {
                // Create ApplicationUser
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    UserType = request.UserType
                };

                var identityResult = await _userManager.CreateAsync(user, request.Password);
                if (!identityResult.Succeeded)
                {
                    _logger.LogWarning("User creation failed for {Email}: {Errors}",
                        request.Email, identityResult.Errors.Select(e => e.Description));
                    return new RegisterResult(false, identityResult.Errors.Select(e => e.Description));
                }

                // Assign role
                var role = request.UserType == "Admin" ? "Admin" : "Customer";
                var roleResult = await _userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to assign role {role}");
                }

                // Create domain-specific profile
                if (request.UserType == "Customer")
                {
                    var customer = new Customer
                    {
                        Address = request.Address!,
                        DateOfBirth = request.DateOfBirth!.Value,
                        Age = CalculateAge(request.DateOfBirth.Value),
                        ApplicationUserId = user.Id,
                        CreatedBy = user.FullName
                    };

                    _logger.LogInformation("Adding Customer profile for {UserId}", user.Id);
                    _unitOfWork.GetRepository<Customer>().Insert(customer);
                }
                else if (request.UserType == "Admin")
                {
                    var admin = new Admin
                    {
                        Department = request.Department!,
                        UserId = user.Id,
                        CreatedBy = user.FullName
                    };

                    _logger.LogInformation("Adding Admin profile for {UserId}", user.Id);
                    _unitOfWork.GetRepository<Admin>().Insert(admin);
                }

                await _unitOfWork.SaveAsync(cancellationToken);
                transaction.Commit();

                _logger.LogInformation("Registration successful for {Email}", request.Email);
                return new RegisterResult(true);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error occurred during registration for {Email}", request.Email);
                throw;
            }
        }

        private int CalculateAge(DateTime dob)
        {
            var today = DateTime.Today;
            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age;
        }
    }

}
