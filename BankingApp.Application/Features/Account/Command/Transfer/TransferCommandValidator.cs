

using BankingApp.Infrastructure.Persistence.UnitOfWork;
using FluentValidation;

namespace BankingApp.Application.Features.Account.Command.Transfer
{
    public class TransferCommandValidator : AbstractValidator<TransferCommand>
    {
        private readonly IUnitOfWork _uow;

        public TransferCommandValidator(IUnitOfWork uow)
        {
            _uow = uow;

            RuleFor(x => x.FromAccountId)
                .GreaterThan(0).WithMessage("FromAccountId must be valid.");

            RuleFor(x => x.ToAccountId)
                .GreaterThan(0).WithMessage("ToAccountId must be valid.")
                .NotEqual(x => x.FromAccountId).WithMessage("Cannot transfer to the same account.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Transfer amount must be greater than zero.");

            RuleFor(x => x)
                .Must(HaveSufficientFunds).WithMessage("Insufficient funds in source account.");
        }

        private bool HaveSufficientFunds(TransferCommand cmd)
        {
            var fromAccount = _uow.GetRepository<BankingApp.Domain.Models.Account>().Get()
                .FirstOrDefault(a => a.Id == cmd.FromAccountId && !a.IsDeleted);
            return fromAccount != null && fromAccount.Balance >= cmd.Amount;
        }
    }
}
