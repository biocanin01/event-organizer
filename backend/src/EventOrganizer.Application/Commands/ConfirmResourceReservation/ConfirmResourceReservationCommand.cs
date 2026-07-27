using MediatR;

namespace EventOrganizer.Application.Commands.ConfirmResourceReservation
{
    public sealed record ConfirmResourceReservationCommand(Guid ReservationId) : IRequest;
}
