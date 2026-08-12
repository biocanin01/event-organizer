using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Commands.WithdrawEventBooking
{
    public sealed record WithdrawEventBookingCommand(
        Guid EventId,
        int Version) : IRequest<EventResourceBookingResponse>;
}
