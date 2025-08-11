using BankingApp.Application.Features.Transaction.Queries.GetTransactionHistory;
using BankingApp.Application.Features.Transaction.Queries.MonthlyStatement;
using BankingApp.Domain.BindingModels.Responses;
using BankingApp.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BankingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TransactionController(IMediator mediator)
        {
            _mediator = mediator;
        }


        // GET: api/transaction/history/{accountId}
        [HttpGet("history/{accountId}")]
        public async Task<ActionResult<ServiceResponse<List<BankingApp.Domain.Models.Transaction>>>> GetTransactionHistory(int accountId)
        {
            var query = new GetTransactionHistoryQuery { AccountId = accountId };
            var response = await _mediator.Send(query);

            if (response.StatusCode == ResponseCode.SUCCESSFUL)
                return Ok(response);

            return BadRequest(response);
        }

        // GET: api/transaction/monthly-statement/{accountId}/{year}/{month}
        [HttpGet("monthly-statement/{accountId}/{year}/{month}")]
        public async Task<ActionResult<ServiceResponse<List<BankingApp.Domain.Models.Transaction>>>> GetMonthlyStatement(
            int accountId, int year, int month)
        {
            var query = new GetMonthlyStatementQuery
            {
                AccountId = accountId,
                Year = year,
                Month = month
            };

            var response = await _mediator.Send(query);

            if (response.StatusCode == ResponseCode.SUCCESSFUL)
                return Ok(response);

            return BadRequest(response);
        }
    }
}
