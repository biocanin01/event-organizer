using EventOrganizer.Api.Contracts.Auth;
using EventOrganizer.Application.Commands.LoginUser;
using EventOrganizer.Application.Commands.RegisterUser;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventOrganizer.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly ISender _sender;

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AuthResponse>> Register(
            RegisterRequest request,
            CancellationToken cancellationToken)
        {
            var command = new RegisterUserCommand(
                request.FullName,
                request.Email,
                request.Password);

            var response = await _sender.Send(command, cancellationToken);

            return Ok(response);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AuthResponse>> Login(
            LoginRequest request,
            CancellationToken cancellationToken)
        {
            var command = new LoginUserCommand(
                request.Email,
                request.Password);

            var response = await _sender.Send(command, cancellationToken);

            return Ok(response);
        }
    }
}
