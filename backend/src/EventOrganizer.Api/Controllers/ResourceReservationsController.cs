using EventOrganizer.Api.Authorization;
using EventOrganizer.Api.Contracts.ResourceReservations;
using EventOrganizer.Application.Commands.CancelResourceReservation;
using EventOrganizer.Application.Commands.ConfirmResourceReservation;
using EventOrganizer.Application.Commands.CreateResourceReservation;
using EventOrganizer.Application.Commands.RejectResourceReservation;
using EventOrganizer.Application.Queries.GetResourceReservationById;
using EventOrganizer.Application.Queries.ListResourceReservations;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventOrganizer.Api.Controllers
{
    [ApiController]
    [Route("api/resource-reservations")]
    public sealed class ResourceReservationsController : ControllerBase
    {
        private readonly ISender _sender;

        public ResourceReservationsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.CanManageResourceReservations)]
        [ProducesResponseType(typeof(IReadOnlyList<ResourceReservationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyList<ResourceReservationResponse>>> List(
            CancellationToken cancellationToken)
        {
            var reservations = await _sender.Send(
                new ListResourceReservationsQuery(),
                cancellationToken);

            return Ok(reservations);
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = AuthorizationPolicies.CanManageResourceReservations)]
        [ProducesResponseType(typeof(ResourceReservationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResourceReservationResponse>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var reservation = await _sender.Send(
                new GetResourceReservationByIdQuery(id),
                cancellationToken);

            if (reservation is null)
            {
                return NotFound();
            }

            return Ok(reservation);
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.CanCreateResourceReservations)]
        [ProducesResponseType(typeof(CreateResourceReservationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<CreateResourceReservationResponse>> Create(
            CreateResourceReservationRequest request,
            CancellationToken cancellationToken)
        {
            var reservationId = await _sender.Send(
                new CreateResourceReservationCommand(
                    request.EventId,
                    request.ResourceId,
                    request.StartsAtUtc,
                    request.EndsAtUtc),
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new CreateResourceReservationResponse(reservationId));
        }

        [HttpPatch("{id:guid}/confirm")]
        [Authorize(Policy = AuthorizationPolicies.CanManageResourceReservations)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Confirm(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new ConfirmResourceReservationCommand(id),
                cancellationToken);

            return NoContent();
        }

        [HttpPatch("{id:guid}/reject")]
        [Authorize(Policy = AuthorizationPolicies.CanManageResourceReservations)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reject(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new RejectResourceReservationCommand(id),
                cancellationToken);

            return NoContent();
        }

        [HttpPatch("{id:guid}/cancel")]
        [Authorize(Policy = AuthorizationPolicies.CanCancelResourceReservations)]
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
                new CancelResourceReservationCommand(id),
                cancellationToken);

            return NoContent();
        }
    }
}
