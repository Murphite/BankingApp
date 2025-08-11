using FluentValidation;

namespace BankingApp.Application.Features.Transaction.Queries.GetTransactionHistory
{
    public class GetTransactionHistoryQueryValidator : AbstractValidator<GetTransactionHistoryQuery>
    {
        public GetTransactionHistoryQueryValidator()
        {
            RuleFor(x => x.AccountId).GreaterThan(0).WithMessage("Account ID must be valid.");
        }
    }
}
