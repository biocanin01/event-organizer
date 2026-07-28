using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.GetResourceReservationById
{
    public sealed record GetResourceReservationByIdQuery(Guid ReservationId)
        : IRequest<ResourceReservationResponse?>;
}
