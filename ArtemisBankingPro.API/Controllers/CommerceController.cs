using ArtemisBankingPro.Core.Application.Commerces.ChangeCommerceStatus;
using ArtemisBankingPro.Core.Application.Commerces.CreateCommerce;
using ArtemisBankingPro.Core.Application.Commerces.GetCommerceById;
using ArtemisBankingPro.Core.Application.Commerces.GetCommerces;
using ArtemisBankingPro.Core.Application.Commerces.UpdateCommerce;
using ArtemisBankingPro.Core.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace ArtemisBankingPro.API.Controllers
{
    [Route("api/commerce")]
    [ApiController]
    [Authorize(Roles = DomainConstants.RolAdministrador)]
    public class CommerceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CommerceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetCommerces([FromQuery] GetCommercesQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCommerceById(int id)
        {
            var result = await _mediator.Send(new GetCommerceByIdQuery(id));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCommerce([FromBody] CreateCommerceRequest request)
        {
            var adminId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value!;

            var command = new CreateCommerceCommand(
                request.Name,
                request.Description,
                request.Email,
                request.PhoneNumber,
                request.Rnc,
                adminId
            );

            var result = await _mediator.Send(command);

            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCommerce(int id, [FromBody] UpdateCommerceRequest request)
        {
            var command = new UpdateCommerceCommand(
                id,
                request.Name,
                request.Description,
                request.Email,
                request.PhoneNumber,
                request.Rnc
            );

            await _mediator.Send(command);

            return NoContent();
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ChangeCommerceStatus(int id, [FromBody] ChangeCommerceStatusRequest request)
        {
            await _mediator.Send(new ChangeCommerceStatusCommand(id, request.Status));
            return NoContent();
        }

        public record CreateCommerceRequest(
            string Name,
            string? Description,
            string Email,
            string PhoneNumber,
            string Rnc);

        public record UpdateCommerceRequest(
            string Name,
            string? Description,
            string Email,
            string PhoneNumber,
            string Rnc);

        public record ChangeCommerceStatusRequest(bool Status);
    }
}