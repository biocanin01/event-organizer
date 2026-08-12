using EventOrganizer.Api.Authorization;
using EventOrganizer.Api.Contracts.Bookings;
using EventOrganizer.Application.Commands.ApproveEventBooking;
using EventOrganizer.Application.Commands.ExpireEventBookings;
using EventOrganizer.Application.Commands.RejectEventBooking;
using EventOrganizer.Application.Queries.GetEventBookingById;
using EventOrganizer.Application.Queries.ListEventBookings;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Bookings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventOrganizer.Api.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    [Authorize(Policy = AuthorizationPolicies.CanManageBookings)]
    public sealed class BookingsController : ControllerBase
    {
        private readonly ISender _sender;

        public BookingsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<EventResourceBookingResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyList<EventResourceBookingResponse>>> List(
            EventResourceBookingStatus? status,
            CancellationToken cancellationToken)
        {
            var bookings = await _sender.Send(
                new ListEventBookingsQuery(status),
                cancellationToken);

            return Ok(bookings);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(EventResourceBookingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventResourceBookingResponse>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var booking = await _sender.Send(
                new GetEventBookingByIdQuery(id),
                cancellationToken);

            return Ok(booking);
        }

        [HttpPatch("{id:guid}/approve")]
        [ProducesResponseType(typeof(EventResourceBookingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EventResourceBookingResponse>> Approve(
            Guid id,
            ApproveEventBookingRequest request,
            CancellationToken cancellationToken)
        {
            var booking = await _sender.Send(
                new ApproveEventBookingCommand(id, request.Version),
                cancellationToken);

            return Ok(booking);
        }

        [HttpPatch("{id:guid}/reject")]
        [ProducesResponseType(typeof(EventResourceBookingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EventResourceBookingResponse>> Reject(
            Guid id,
            RejectEventBookingRequest request,
            CancellationToken cancellationToken)
        {
            var booking = await _sender.Send(
                new RejectEventBookingCommand(id, request.Reason, request.Version),
                cancellationToken);

            return Ok(booking);
        }

        [HttpPatch("expire")]
        [ProducesResponseType(typeof(ExpireEventBookingsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ExpireEventBookingsResponse>> Expire(
            CancellationToken cancellationToken)
        {
            var expiredCount = await _sender.Send(
                new ExpireEventBookingsCommand(),
                cancellationToken);

            return Ok(new ExpireEventBookingsResponse(expiredCount));
        }
    }
}
