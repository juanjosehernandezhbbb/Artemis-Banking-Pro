using ArtemisBankingPro.Core.Application.Account.ConfirmAccount;
using ArtemisBankingPro.Core.Application.Account.GetResetToken;
using ArtemisBankingPro.Core.Application.Account.Login;
using ArtemisBankingPro.Core.Application.Account.ResetPassword;
using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ArtemisBankingPro.API.Controllers
{
    [Route("account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public AccountController(IMediator mediator, IJwtTokenGenerator jwtTokenGenerator)
        {
            _mediator = mediator;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var allowedRoles = new[] { DomainConstants.RolAdministrador, DomainConstants.RolComercio };
            var command = new LoginCommand(request.UserName, request.Password, allowedRoles);

            var result = await _mediator.Send(command);

            var jwt = _jwtTokenGenerator.GenerateToken(result.UserId, result.UserName, result.Role, result.CommerceId);

            return Ok(new { jwt });
        }

        [HttpPost("confirm")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmAccount([FromBody] ConfirmAccountCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpPost("get-reset-token")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var allowedRoles = new[] { DomainConstants.RolAdministrador, DomainConstants.RolComercio };
            await _mediator.Send(new GetResetTokenCommand(request.UserName, allowedRoles));

            return NoContent();
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }
    }

    public record LoginRequest(string UserName, string Password);
    public record ForgotPasswordRequest(string UserName);
}