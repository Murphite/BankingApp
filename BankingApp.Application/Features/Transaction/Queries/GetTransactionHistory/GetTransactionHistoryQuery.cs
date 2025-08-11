using BankingApp.Domain.BindingModels.Responses;
using MediatR;

namespace BankingApp.Application.Features.Transaction.Queries.GetTransactionHistory
{
    public class GetTransactionHistoryQuery : IRequest<ServiceResponse<List<BankingApp.Domain.Models.Transaction>>>
    {
        public long AccountId { get; set; }
    }
}
