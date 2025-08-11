using MediatR;

namespace BankingApp.Application.Features.Auth.Commands.Register
{
    public record RegisterCommand(
        string Email,
        string Password,
        string FullName,
        string UserType,
        string? PhoneNumber,
        string? Address = null,
        DateTime? DateOfBirth = null,
        string? Department = null
    ) : IRequest<RegisterResult>;

    public record RegisterResult(bool Success, IEnumerable<string>? Errors = null);
}
