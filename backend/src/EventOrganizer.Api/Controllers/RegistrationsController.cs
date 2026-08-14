using EventOrganizer.Api.Contracts.Registrations;
using EventOrganizer.Application.Commands.CancelRegistration;
using EventOrganizer.Application.Commands.ConfirmRegistration;
using EventOrganizer.Application.Commands.CreateEventRegistration;
using EventOrganizer.Application.Commands.RejectRegistration;
using EventOrganizer.Application.Queries.ListEventRegistrations;
using EventOrganizer.Application.Queries.ListMyRegistrations;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Registrations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventOrganizer.Api.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public sealed class RegistrationsController : ControllerBase
    {
        private readonly ISender _sender;

        public RegistrationsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("events/{eventId:guid}/registrations")]
        [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<RegistrationResponse>> Create(
            Guid eventId,
            CancellationToken cancellationToken)
        {
            var registration = await _sender.Send(
                new CreateEventRegistrationCommand(eventId),
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, registration);
        }

        [HttpGet("registrations/me")]
        [ProducesResponseType(typeof(IReadOnlyList<RegistrationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IReadOnlyList<RegistrationResponse>>> ListMine(
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ListMyRegistrationsQuery(), cancellationToken));
        }

        [HttpGet("events/{eventId:guid}/registrations")]
        [ProducesResponseType(typeof(IReadOnlyList<RegistrationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<RegistrationResponse>>> ListForEvent(
            Guid eventId,
            RegistrationStatus? status,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(
                new ListEventRegistrationsQuery(eventId, status),
                cancellationToken));
        }

        [HttpPatch("registrations/{id:guid}/cancel")]
        [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<RegistrationResponse>> Cancel(
            Guid id,
            RegistrationVersionRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(
                new CancelRegistrationCommand(id, request.Version),
                cancellationToken));
        }

        [HttpPatch("registrations/{id:guid}/confirm")]
        [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<RegistrationResponse>> Confirm(
            Guid id,
            RegistrationVersionRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(
                new ConfirmRegistrationCommand(id, request.Version),
                cancellationToken));
        }

        [HttpPatch("registrations/{id:guid}/reject")]
        [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<RegistrationResponse>> Reject(
            Guid id,
            RejectRegistrationRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(
                new RejectRegistrationCommand(id, request.Reason, request.Version),
                cancellationToken));
        }
    }
}
