using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Commands.RejectEventBooking
{
    public sealed record RejectEventBookingCommand(
        Guid BookingId,
        string? DecisionReason,
        int Version) : IRequest<EventResourceBookingResponse>;
}
