using BankingApp.Application.Features.Auth.Commands.Login;
using BankingApp.Domain.Models;
using BankingApp.Domain.Utility.Interfaces;
using BankingApp.Infrastructure.Persistence.Context;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MediatR;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly BankingDbContext _db;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtService jwtService,
        ILogger<LoginCommandHandler> logger,
        BankingDbContext db)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _logger = logger;
        _db = db;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting login for {Email}", request.Email);

        // ✅ Validation step
        var validator = new LoginCommandValidator();
        var validationResult = validator.Validate(request);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for login: {Errors}", validationResult.Errors);
            throw new ValidationException(validationResult.Errors);
        }

        var user = await _userManager.Users
            .Include(u => u.CustomerDetails)
            .Include(u => u.AdminDetails)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User {Email} not found", request.Email);
            return new LoginResult(false, ErrorMessage: "Invalid credentials");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User {Email} is inactive", request.Email);
            return new LoginResult(false, ErrorMessage: "Account inactive");
        }

        var valid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
        {
            _logger.LogWarning("Login failed: Invalid password for {Email}", request.Email);
            return new LoginResult(false, ErrorMessage: "Invalid credentials");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtService.GenerateToken(user, roles);

        object? profile = null;
        if (user.UserType == "Customer" && user.CustomerDetails != null)
        {
            profile = new
            {
                user.CustomerDetails.Id,
                user.CustomerDetails.Address,
                user.CustomerDetails.DateOfBirth,
                user.CustomerDetails.Age
            };
        }
        else if (user.UserType == "Admin" && user.AdminDetails != null)
        {
            profile = new
            {
                user.AdminDetails.Id,
                user.AdminDetails.Department
            };
        }

        _logger.LogInformation("User {Email} logged in successfully", request.Email);

        return new LoginResult(true, Token: token, Roles: roles, Profile: profile);
    }
}
