using EventOrganizer.Api.Contracts.Auth;
using EventOrganizer.Api.Auth;
using EventOrganizer.Application.Commands.LoginUser;
using EventOrganizer.Application.Commands.LogoutUser;
using EventOrganizer.Application.Commands.RefreshToken;
using EventOrganizer.Application.Commands.RegisterUser;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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

            AppendRefreshTokenCookie(response);

            return Ok(RemoveRefreshTokenFromResponse(response));
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

            AppendRefreshTokenCookie(response);

            return Ok(RemoveRefreshTokenFromResponse(response));
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> RefreshToken(
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]
            RefreshTokenRequest? request,
            CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies[RefreshTokenCookie.Name]
                ?? request?.RefreshToken;

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return Unauthorized();
            }

            var command = new RefreshTokenCommand(refreshToken);

            var response = await _sender.Send(command, cancellationToken);

            AppendRefreshTokenCookie(response);

            return Ok(RemoveRefreshTokenFromResponse(response));
        }

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout(
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]
            LogoutRequest? request,
            CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies[RefreshTokenCookie.Name]
                ?? request?.RefreshToken;

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                Response.Cookies.Delete(
                    RefreshTokenCookie.Name,
                    RefreshTokenCookie.DeleteOptions(Request));

                return NoContent();
            }

            await _sender.Send(
                new LogoutUserCommand(refreshToken),
                cancellationToken);

            Response.Cookies.Delete(
                RefreshTokenCookie.Name,
                RefreshTokenCookie.DeleteOptions(Request));

            return NoContent();
        }

        private void AppendRefreshTokenCookie(AuthResponse response)
        {
            if (response.RefreshToken is null || response.RefreshTokenExpiresAtUtc is null)
            {
                return;
            }

            Response.Cookies.Append(
                RefreshTokenCookie.Name,
                response.RefreshToken,
                RefreshTokenCookie.CreateOptions(
                    Request,
                    response.RefreshTokenExpiresAtUtc.Value));
        }

        private static AuthResponse RemoveRefreshTokenFromResponse(AuthResponse response)
        {
            return response with
            {
                RefreshToken = null,
                RefreshTokenExpiresAtUtc = null,
            };
        }
    }
}
