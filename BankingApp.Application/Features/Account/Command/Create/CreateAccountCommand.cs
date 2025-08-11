
using MediatR;
using BankingApp.Domain.BindingModels.Responses;

namespace BankingApp.Application.Features.Account.Command.Create
{
    public class CreateAccountCommand : IRequest<ServiceResponse>
    {
        public string AccountHolderName { get; set; }
        public decimal InitialDeposit { get; set; }
    }
}
