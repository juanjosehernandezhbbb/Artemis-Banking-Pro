using ArtemisBankingPro.Core.Application.SavingsAccounts.CancelSavingsAccount;
using ArtemisBankingPro.Core.Application.SavingsAccounts.CreateSavingsAccount;
using ArtemisBankingPro.Core.Application.SavingsAccounts.GetAccountTransactions;
using ArtemisBankingPro.Core.Application.SavingsAccounts.GetSavingsAccounts;
using ArtemisBankingPro.Core.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace ArtemisBankingPro.API.Controllers
{
    [Route("api/savings-account")]
    [ApiController]
    [Authorize(Roles = DomainConstants.RolAdministrador)]
    public class SavingsAccountController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SavingsAccountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetSavingsAccounts([FromQuery] GetSavingsAccountsQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSavingsAccount([FromBody] CreateSavingsAccountRequest request)
        {
            var adminId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value!;

            var command = new CreateSavingsAccountCommand(request.ClientId, request.InitialBalance, adminId);
            var result = await _mediator.Send(command);

            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpGet("{accountNumber}/transactions")]
        public async Task<IActionResult> GetAccountTransactions(string accountNumber, [FromQuery] GetAccountTransactionsQuery query)
        {
            var result = await _mediator.Send(query with { AccountNumber = accountNumber });
            return Ok(result);
        }

        [HttpPatch("{accountNumber}/cancel")]
        public async Task<IActionResult> CancelSavingsAccount(string accountNumber)
        {
            var adminId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value!;
            await _mediator.Send(new CancelSavingsAccountCommand(accountNumber, adminId));
            return NoContent();
        }

        public record CreateSavingsAccountRequest(string ClientId, decimal InitialBalance);
    }
}