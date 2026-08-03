using EventOrganizer.Api.Authorization;
using EventOrganizer.Application.Commands.ReactivateUser;
using EventOrganizer.Application.Commands.SuspendUser;
using EventOrganizer.Application.Queries.GetUserById;
using EventOrganizer.Application.Queries.ListUsers;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventOrganizer.Api.Controllers
{
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.CanManageUsers)]
    [Route("api/admin/users")]
    public sealed class AdminUsersController : ControllerBase
    {
        private readonly ISender _sender;

        public AdminUsersController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyList<UserResponse>>> List(
            string? search,
            UserStatus? status,
            string? role,
            CancellationToken cancellationToken)
        {
            var users = await _sender.Send(
                new ListUsersQuery(search, status, role),
                cancellationToken);

            return Ok(users);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(UserDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDetailsResponse>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var user = await _sender.Send(
                new GetUserByIdQuery(id),
                cancellationToken);

            return Ok(user);
        }

        [HttpPatch("{id:guid}/suspend")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Suspend(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new SuspendUserCommand(id),
                cancellationToken);

            return NoContent();
        }

        [HttpPatch("{id:guid}/reactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Reactivate(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new ReactivateUserCommand(id),
                cancellationToken);

            return NoContent();
        }
    }
}
