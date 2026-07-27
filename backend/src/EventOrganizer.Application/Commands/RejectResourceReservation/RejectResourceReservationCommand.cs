using MediatR;

namespace EventOrganizer.Application.Commands.RejectResourceReservation
{
    public sealed record RejectResourceReservationCommand(Guid ReservationId) : IRequest;
}
