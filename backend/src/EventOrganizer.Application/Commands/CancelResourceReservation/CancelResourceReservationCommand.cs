using MediatR;

namespace EventOrganizer.Application.Commands.CancelResourceReservation
{
    public sealed record CancelResourceReservationCommand(Guid ReservationId) : IRequest;
}
