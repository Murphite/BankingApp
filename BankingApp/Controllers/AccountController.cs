
using BankingApp.Application.Features.Account.Command.Create;
using BankingApp.Application.Features.Account.Command.Deposit;
using BankingApp.Application.Features.Account.Command.Transfer;
using BankingApp.Application.Features.Account.Command.Withdraw;
using BankingApp.Application.Features.Account.Queries.GetById;
using BankingApp.Domain.BindingModels.Responses;
using BankingApp.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AccountsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        // POST: api/accounts
        [HttpPost]
        public async Task<ActionResult<ServiceResponse>> CreateAccount([FromBody] CreateAccountCommand command)
        {
            var response = await _mediator.Send(command);
            if (response.StatusCode == ResponseCode.SUCCESSFUL)
                return Ok(response);
            return BadRequest(response);
        }

        // POST: api/accounts/deposit
        [HttpPost("deposit")]
        public async Task<ActionResult<bool>> Deposit([FromBody] DepositCommand command)
        {
            var success = await _mediator.Send(command);
            if (success) return Ok(true);
            return BadRequest(false);
        }

        // POST: api/accounts/withdraw
        [HttpPost("withdraw")]
        public async Task<ActionResult<bool>> Withdraw([FromBody] WithdrawCommand command)
        {
            var success = await _mediator.Send(command);
            if (success) return Ok(true);
            return BadRequest(false);
        }

        // POST: api/accounts/transfer
        [HttpPost("transfer")]
        public async Task<ActionResult<ServiceResponse>> Transfer([FromBody] TransferCommand command)
        {
            var response = await _mediator.Send(command);
            if (response.StatusCode == ResponseCode.SUCCESSFUL)
                return Ok(response);
            return BadRequest(response);
        }

        // GET: api/accounts/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceResponse<BankingApp.Domain.Models.Account>>> GetById(long id)
        {
            var query = new GetAccountByIdQuery { AccountId = id };
            var response = await _mediator.Send(query);
            if (response.StatusCode == ResponseCode.SUCCESSFUL)
                return Ok(response);
            return NotFound(response);
        }
    }
}
