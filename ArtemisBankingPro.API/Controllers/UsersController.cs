using ArtemisBankingPro.Core.Application.Users.ChangeUserStatus;
using ArtemisBankingPro.Core.Application.Users.CreateCommerceUser;
using ArtemisBankingPro.Core.Application.Users.CreateUser;
using ArtemisBankingPro.Core.Application.Users.GetCommerces;
using ArtemisBankingPro.Core.Application.Users.GetUserById;
using ArtemisBankingPro.Core.Application.Users.GetUsers;
using ArtemisBankingPro.Core.Application.Users.UpdateUser;
using ArtemisBankingPro.Core.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace ArtemisBankingPro.API.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize(Roles = DomainConstants.RolAdministrador)]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var command = new CreateUserCommand(
                request.FirstName,
                request.LastName,
                request.Identification,
                request.Email,
                request.UserName,
                request.Password,
                request.ConfirmPassword,
                request.Role,
                request.InitialAmount
            );

            var userId = await _mediator.Send(command);

            return StatusCode(StatusCodes.Status201Created, new
            {
                id = userId,
                userName = request.UserName,
                email = request.Email,
                role = request.Role,
                isActive = false
            });
        }

        
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var query = new GetUserByIdQuery(id);
            var result = await _mediator.Send(query);

            return Ok(result);
        }

        [HttpGet("commerce")]
        public async Task<IActionResult> GetCommerces([FromQuery] GetCommercesQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost("commerce/{commerceId:int}")]
        public async Task<IActionResult> CreateCommerceUser(int commerceId, [FromBody] CreateCommerceUserRequest request)
        {
            var command = new CreateCommerceUserCommand(
                request.FirstName,
                request.LastName,
                request.Identification,
                request.Email,
                request.UserName,
                request.Password,
                request.ConfirmPassword,
                commerceId,
                request.InitialAmount
            );

            var userId = await _mediator.Send(command);

            return StatusCode(StatusCodes.Status201Created, new
            {
                id = userId,
                userName = request.UserName,
                email = request.Email,
                role = DomainConstants.RolComercio,
                commerceId,
                isActive = false
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            var adminId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value!;

            var command = new UpdateUserCommand(
                id,
                request.FirstName,
                request.LastName,
                request.Identification,
                request.Email,
                request.UserName,
                request.Password,
                request.ConfirmPassword,
                request.AdditionalAmount,
                adminId 
            );

            await _mediator.Send(command);

            return NoContent();
        }

        public record UpdateUserRequest(
            string FirstName,
            string LastName,
            string Identification,
            string Email,
            string UserName,
            string? Password,
            string? ConfirmPassword,
            decimal AdditionalAmount = 0
        );

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeUserStatus(string id, [FromBody] ChangeUserStatusRequest request)
        {
            var adminId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value!;

            var command = new ChangeUserStatusCommand(id, request.Status, adminId);
            await _mediator.Send(command);

            return NoContent();
        }

        public record ChangeUserStatusRequest(bool? Status);


    }

    public record CreateUserRequest(
        string FirstName,
        string LastName,
        string Identification,
        string Email,
        string UserName,
        string Password,
        string ConfirmPassword,
        string Role,
        decimal InitialAmount = 0
    );

    public record CreateCommerceUserRequest(
        string FirstName,
        string LastName,
        string Identification,
        string Email,
        string UserName,
        string Password,
        string ConfirmPassword,
        decimal InitialAmount = 0
    );
}