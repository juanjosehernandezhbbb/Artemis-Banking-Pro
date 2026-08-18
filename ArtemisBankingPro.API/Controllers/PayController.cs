using ArtemisBankingPro.Core.Application.HermesPay.GetCommerceTransactions;
using ArtemisBankingPro.Core.Application.HermesPay.ProcessPayment;
using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtemisBankingPro.API.Controllers
{
    [Route("pay")]
    [ApiController]
    [Authorize(Roles = $"{DomainConstants.RolAdministrador},{DomainConstants.RolComercio}")]
    public class PayController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PayController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("get-transactions/{commerceId:int}")]
        public async Task<IActionResult> GetCommerceTransactions(int commerceId, [FromQuery] GetCommerceTransactionsQuery query)
        {
            var effectiveCommerceId = ResolveCommerceId(commerceId);
            var result = await _mediator.Send(query with { CommerceId = effectiveCommerceId });
            return Ok(result);
        }

        [HttpPost("process-payment/{commerceId:int}")]
        public async Task<IActionResult> ProcessPayment(int commerceId, [FromBody] ProcessPaymentRequest request)
        {
            var effectiveCommerceId = ResolveCommerceId(commerceId);

            var command = new ProcessPaymentCommand(
                effectiveCommerceId,
                request.CardNumber,
                request.MonthExpirationCard,
                request.YearExpirationCard,
                request.Cvc,
                request.TransactionAmount
            );

            var result = await _mediator.Send(command);

            if (!result.Approved)
            {
                return BadRequest(new { message = result.Message });
            }

            return NoContent();
        }

        private int ResolveCommerceId(int urlCommerceId)
        {
            if (User.IsInRole(DomainConstants.RolComercio))
            {
                var commerceIdClaim = User.FindFirst("commerceId")?.Value;
                if (string.IsNullOrWhiteSpace(commerceIdClaim) || !int.TryParse(commerceIdClaim, out var tokenCommerceId))
                {
                    throw new ForbiddenAccessException("El usuario no tiene un comercio asociado.");
                }

                return tokenCommerceId;
            }

            return urlCommerceId;
        }

        public record ProcessPaymentRequest(
            string CardNumber,
            string MonthExpirationCard,
            string YearExpirationCard,
            string Cvc,
            decimal TransactionAmount);
    }
}