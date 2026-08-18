using ArtemisBankingPro.Core.Application.Loans.CreateLoan;
using ArtemisBankingPro.Core.Application.Loans.GetLoanById;
using ArtemisBankingPro.Core.Application.Loans.GetLoans;
using ArtemisBankingPro.Core.Application.Loans.UpdateLoanRate;
using ArtemisBankingPro.Core.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace ArtemisBankingPro.API.Controllers
{
    [Route("api/loan")]
    [ApiController]
    [Authorize(Roles = DomainConstants.RolAdministrador)]
    public class LoanController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LoanController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetLoans([FromQuery] GetLoansQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLoan([FromBody] CreateLoanRequest request)
        {
            var adminId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value!;

            var command = new CreateLoanCommand(
                request.ClientId,
                request.CapitalAmount,
                request.TermInMonths,
                request.AnnualInterestRate,
                request.ConfirmHighRisk,
                adminId
            );

            var result = await _mediator.Send(command);

            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetLoanById(int id)
        {
            var result = await _mediator.Send(new GetLoanByIdQuery(id));
            return Ok(result);
        }

        [HttpPatch("{id:int}/rate")]
        public async Task<IActionResult> UpdateLoanRate(int id, [FromBody] UpdateLoanRateRequest request)
        {
            await _mediator.Send(new UpdateLoanRateCommand(id, request.AnnualInterestRate));
            return NoContent();
        }

        public record CreateLoanRequest(
            string ClientId,
            decimal CapitalAmount,
            int TermInMonths,
            decimal AnnualInterestRate,
            bool ConfirmHighRisk = false
        );

        public record UpdateLoanRateRequest(decimal AnnualInterestRate);
    }
}