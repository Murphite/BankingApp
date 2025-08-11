

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
            // Now we query by AccountNumber instead of Id
            var fromAccount = _uow.GetRepository<BankingApp.Domain.Models.Account>().Get()
                .FirstOrDefault(a => a.AccountNumber == cmd.FromAccountId && !a.IsDeleted);

            if (fromAccount == null)
            {
                Console.WriteLine($"Account not found for AccountNumber: {cmd.FromAccountId}");
                return false;
            }

            Console.WriteLine($"FromAccount Balance: {fromAccount.Balance}, Amount Requested: {cmd.Amount}");
            return fromAccount.Balance >= cmd.Amount;
        }
    }
}
