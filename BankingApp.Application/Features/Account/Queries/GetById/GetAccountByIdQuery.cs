

using BankingApp.Domain.BindingModels.Responses;
using MediatR;

namespace BankingApp.Application.Features.Account.Queries.GetById
{
    public class GetAccountByIdQuery
    : IRequest<ServiceResponse<BankingApp.Domain.Models.Account>>
    {
        public long AccountId { get; set; }
    }
}
