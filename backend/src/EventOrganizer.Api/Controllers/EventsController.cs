using EventOrganizer.Api.Authorization;
using EventOrganizer.Api.Contracts.Events;
using EventOrganizer.Application.Commands.CreateEvent;
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

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.CanCreateEvents)]
        [ProducesResponseType(typeof(CreateEventResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CreateEventResponse>> Create(CreateEventRequest request, CancellationToken cancellationToken)
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
    }
}
