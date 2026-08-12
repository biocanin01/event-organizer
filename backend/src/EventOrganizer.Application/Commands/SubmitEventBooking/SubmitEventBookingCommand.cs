using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Commands.SubmitEventBooking
{
    public sealed record SubmitEventBookingCommand(
        Guid EventId,
        int Version) : IRequest<EventResourceBookingResponse>;
}
