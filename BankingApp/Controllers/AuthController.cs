using BankingApp.Application.Features.Auth.Commands.Login;
using BankingApp.Application.Features.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BankingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResult>> Login([FromBody] LoginCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                if (result.Success)
                    return Ok(result);
                return Unauthorized(result);
            }
            catch (FluentValidation.ValidationException ex)
            {
                return BadRequest(new { Errors = ex.Errors });
            }
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<RegisterResult>> Register([FromBody] RegisterCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                if (result.Success)
                    return Ok(result);
                return BadRequest(result);
            }
            catch (FluentValidation.ValidationException ex)
            {
                return BadRequest(new { Errors = ex.Errors });
            }
        }

    }
}
