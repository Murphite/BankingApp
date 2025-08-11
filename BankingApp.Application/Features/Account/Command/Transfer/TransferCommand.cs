

using BankingApp.Domain.BindingModels.Responses;
using MediatR;

namespace BankingApp.Application.Features.Account.Command.Transfer
{
    public class TransferCommand : IRequest<ServiceResponse>
    {
        public long FromAccountId { get; set; }
        public long ToAccountId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}
