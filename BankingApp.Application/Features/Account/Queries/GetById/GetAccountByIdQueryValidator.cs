
using FluentValidation;

namespace BankingApp.Application.Features.Account.Queries.GetById
{
    public class GetAccountByIdQueryValidator : AbstractValidator<GetAccountByIdQuery>
    {
        public GetAccountByIdQueryValidator()
        {
            RuleFor(x => x.AccountId)
                .GreaterThan(0).WithMessage("Account ID must be greater than zero.");
        }
    }
}
