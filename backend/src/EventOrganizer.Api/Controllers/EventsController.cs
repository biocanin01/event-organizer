using EventOrganizer.Api.Authorization;
using EventOrganizer.Api.Contracts.Events;
using EventOrganizer.Application.Commands.CancelEvent;
using EventOrganizer.Application.Commands.CreateEvent;
using EventOrganizer.Application.Commands.PublishEvent;
using EventOrganizer.Application.Queries.GetPublishedEventById;
using EventOrganizer.Application.Queries.ListPublishedEvents;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventOrganizer.Api.Controllers
{
    [ApiController]
    [Route("api/events")]
    public sealed class EventsController : ControllerBase
    {
        private readonly ISender _sender;

        public EventsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<EventResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<EventResponse>>> ListPublished(
            CancellationToken cancellationToken)
        {
            var events = await _sender.Send(
                new ListPublishedEventsQuery(),
                cancellationToken);

            return Ok(events);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventResponse>> GetPublishedById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var eventItem = await _sender.Send(
                new GetPublishedEventByIdQuery(id),
                cancellationToken);

            return Ok(eventItem);
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.CanCreateEvents)]
        [ProducesResponseType(typeof(CreateEventResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CreateEventResponse>> Create(
            CreateEventRequest request,
            CancellationToken cancellationToken)
        {
            var command = new CreateEventCommand(
                request.Title,
                request.Description,
                request.StartsAtUtc,
                request.EndsAtUtc,
                request.Capacity);

            var eventId = await _sender.Send(command, cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new CreateEventResponse(eventId));
        }

        [HttpPatch("{id:guid}/publish")]
        [Authorize(Policy = AuthorizationPolicies.CanManageEvents)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Publish(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new PublishEventCommand(id),
                cancellationToken);

            return NoContent();
        }

        [HttpPatch("{id:guid}/cancel")]
        [Authorize(Policy = AuthorizationPolicies.CanManageEvents)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new CancelEventCommand(id),
                cancellationToken);

            return NoContent();
        }
    }
}
