using MediatR;

namespace EventOrganizer.Application.Commands.CreateResourceReservation
{
    public sealed record CreateResourceReservationCommand(
        Guid EventId,
        Guid ResourceId,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc) : IRequest<Guid>;
}