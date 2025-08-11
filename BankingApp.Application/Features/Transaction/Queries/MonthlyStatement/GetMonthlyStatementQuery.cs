

using BankingApp.Domain.BindingModels.Responses;
using MediatR;

namespace BankingApp.Application.Features.Transaction.Queries.MonthlyStatement
{
    public class GetMonthlyStatementQuery
    : IRequest<ServiceResponse<List<BankingApp.Domain.Models.Transaction>>>
    {
        public long AccountId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
    }

}
