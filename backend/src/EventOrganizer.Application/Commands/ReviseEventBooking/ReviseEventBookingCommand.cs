using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Commands.ReviseEventBooking
{
    public sealed record ReviseEventBookingCommand(
        Guid EventId,
        int Version) : IRequest<EventResourceBookingResponse>;
}
