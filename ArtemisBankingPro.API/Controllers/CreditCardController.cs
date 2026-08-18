using ArtemisBankingPro.Core.Application.CreditCards.CancelCreditCard;
using ArtemisBankingPro.Core.Application.CreditCards.CreateCreditCard;
using ArtemisBankingPro.Core.Application.CreditCards.GetCreditCardById;
using ArtemisBankingPro.Core.Application.CreditCards.GetCreditCards;
using ArtemisBankingPro.Core.Application.CreditCards.UpdateCreditCardLimit;
using ArtemisBankingPro.Core.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace ArtemisBankingPro.API.Controllers
{
    [Route("api/credit-card")]
    [ApiController]
    [Authorize(Roles = DomainConstants.RolAdministrador)]
    public class CreditCardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CreditCardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetCreditCards([FromQuery] GetCreditCardsQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCreditCard([FromBody] CreateCreditCardRequest request)
        {
            var adminId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value!;

            var command = new CreateCreditCardCommand(request.ClientId, request.CreditLimit, adminId);
            var result = await _mediator.Send(command);

            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCreditCardById(int id)
        {
            var result = await _mediator.Send(new GetCreditCardByIdQuery(id));
            return Ok(result);
        }

        [HttpPatch("{id:int}/limit")]
        public async Task<IActionResult> UpdateCreditCardLimit(int id, [FromBody] UpdateCreditCardLimitRequest request)
        {
            await _mediator.Send(new UpdateCreditCardLimitCommand(id, request.CreditLimit));
            return NoContent();
        }

        [HttpPatch("{id:int}/cancel")]
        public async Task<IActionResult> CancelCreditCard(int id)
        {
            await _mediator.Send(new CancelCreditCardCommand(id));
            return NoContent();
        }

        public record CreateCreditCardRequest(string ClientId, decimal CreditLimit);
        public record UpdateCreditCardLimitRequest(decimal CreditLimit);
    }
}