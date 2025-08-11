

using BankingApp.Application.Features.Auth.Commands.Register;
using MediatR;

namespace BankingApp.Application.Features.Auth.Commands.Login
{
    public record LoginResult(
        bool Success,
        string? Token = null,
        IEnumerable<string>? Roles = null,
        string? ErrorMessage = null,
        object? Profile = null
    );


    public record LoginCommand(
        string Email,
        string Password
    ): IRequest<LoginResult>;


}
