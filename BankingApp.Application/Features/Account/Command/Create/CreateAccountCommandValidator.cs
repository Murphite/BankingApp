using BankingApp.Infrastructure.Persistence.UnitOfWork;
using FluentValidation;

namespace BankingApp.Application.Features.Account.Command.Create
{
    public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
    {
        private readonly IUnitOfWork _uow;

        public CreateAccountCommandValidator(IUnitOfWork uow)
        {
            _uow = uow;

            RuleFor(x => x.AccountHolderName)
                .NotEmpty()
                .MaximumLength(150)
                .WithMessage("Account holder name is required and cannot exceed 150 characters.");

            RuleFor(x => x.InitialDeposit)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Initial deposit must be zero or greater.");

            RuleFor(x => x)
                .Must(BeUniqueAccountHolder)
                .WithMessage("An account for this holder already exists.");
        }

        private bool BeUniqueAccountHolder(CreateAccountCommand cmd)
        {
            return !_uow.GetRepository<BankingApp.Domain.Models.Account>().Get()
                .Any(a => a.AccountHolderName.ToLower() == cmd.AccountHolderName.ToLower() && !a.IsDeleted);
        }
    }
}
