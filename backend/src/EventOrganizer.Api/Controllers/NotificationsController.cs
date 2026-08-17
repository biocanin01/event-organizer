using EventOrganizer.Application.Commands.MarkAllNotificationsAsRead;
using EventOrganizer.Application.Commands.MarkNotificationAsRead;
using EventOrganizer.Application.Queries.GetUnreadNotificationCount;
using EventOrganizer.Application.Queries.ListMyNotifications;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventOrganizer.Api.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public sealed class NotificationsController : ControllerBase
    {
        private readonly ISender _sender;

        public NotificationsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<NotificationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> ListMine(
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ListMyNotificationsQuery(), cancellationToken));
        }

        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(UnreadNotificationCountResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UnreadNotificationCountResponse>> GetUnreadCount(
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(
                new GetUnreadNotificationCountQuery(),
                cancellationToken));
        }

        [HttpPatch("{id:guid}/read")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> MarkAsRead(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new MarkNotificationAsReadCommand(id), cancellationToken);
            return NoContent();
        }

        [HttpPatch("read-all")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> MarkAllAsRead(
            CancellationToken cancellationToken)
        {
            await _sender.Send(new MarkAllNotificationsAsReadCommand(), cancellationToken);
            return NoContent();
        }
    }
}
