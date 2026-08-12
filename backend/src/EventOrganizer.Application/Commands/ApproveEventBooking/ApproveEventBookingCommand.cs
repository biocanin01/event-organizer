using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Commands.ApproveEventBooking
{
    public sealed record ApproveEventBookingCommand(Guid BookingId, int Version)
        : IRequest<EventResourceBookingResponse>;
}
