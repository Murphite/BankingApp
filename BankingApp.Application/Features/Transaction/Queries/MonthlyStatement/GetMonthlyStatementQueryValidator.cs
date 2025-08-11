

using FluentValidation;

namespace BankingApp.Application.Features.Transaction.Queries.MonthlyStatement
{
    public class GetMonthlyStatementQueryValidator : AbstractValidator<GetMonthlyStatementQuery>
    {
        public GetMonthlyStatementQueryValidator()
        {
            RuleFor(x => x.AccountId).GreaterThan(0).WithMessage("Account ID must be valid.");
            RuleFor(x => x.Year).InclusiveBetween(2000, DateTime.UtcNow.Year).WithMessage("Invalid year.");
            RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("Invalid month.");
        }
    }
}
