

using MediatR;

namespace BankingApp.Application.Features.Account.Command.Deposit
{
    public class DepositCommand : IRequest<bool>
    {
        public long AccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
