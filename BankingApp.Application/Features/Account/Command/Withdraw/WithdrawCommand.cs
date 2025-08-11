

using MediatR;

namespace BankingApp.Application.Features.Account.Command.Withdraw
{
    public class WithdrawCommand : IRequest<bool>
    {
        public long AccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
