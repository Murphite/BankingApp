using FluentValidation;

namespace BankingApp.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty().MinimumLength(6);

            RuleFor(x => x.FullName)
                .NotEmpty();

            RuleFor(x => x.UserType)
                .NotEmpty()
                .Must(x => x == "Admin" || x == "Customer")
                .WithMessage("UserType must be either 'Admin' or 'Customer'");

            // Conditional validation
            When(x => x.UserType == "Customer", () =>
            {
                RuleFor(x => x.Address).NotEmpty();
                RuleFor(x => x.DateOfBirth).NotNull();
            });

            When(x => x.UserType == "Admin", () =>
            {
                RuleFor(x => x.Department).NotEmpty();
            });
        }
    }
}
