using BankingApp.Domain.Models;
using BankingApp.Infrastructure.Persistence.Context;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BankingApp.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly BankingDbContext _db;
        private readonly ILogger<RegisterCommandHandler> _logger;

        public RegisterCommandHandler(
            UserManager<ApplicationUser> userManager,
            BankingDbContext db,
            ILogger<RegisterCommandHandler> logger)
        {
            _userManager = userManager;
            _db = db;
            _logger = logger;
        }

        public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting registration for {Email}", request.Email);

            // ✅ Validation step
            var validator = new RegisterCommandValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for registration: {Errors}", validationResult.Errors);
                throw new ValidationException(validationResult.Errors);
            }

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
            await _userManager.AddToRoleAsync(user, role);

            try
            {
                if (request.UserType == "Customer")
                {
                    var customer = new Customer
                    {
                        Address = request.Address!,
                        DateOfBirth = request.DateOfBirth!.Value,
                        Age = CalculateAge(request.DateOfBirth.Value),
                        ApplicationUserId = user.Id
                    };
                    _logger.LogInformation("Adding Customer profile for user {UserId}", user.Id);
                    _db.Customers.Add(customer);
                }
                else if (request.UserType == "Admin")
                {
                    var admin = new Admin
                    {
                        Department = request.Department!,
                        UserId = user.Id
                    };
                    _logger.LogInformation("Adding Admin profile for user {UserId}", user.Id);
                    _db.Admins.Add(admin);
                }

                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Domain-specific profile saved successfully for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving domain-specific profile for user {UserId}", user.Id);
                throw; 
            }

            return new RegisterResult(true);
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
