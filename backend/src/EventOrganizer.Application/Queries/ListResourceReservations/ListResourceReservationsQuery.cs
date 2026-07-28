using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.ListResourceReservations
{
    public sealed record ListResourceReservationsQuery
        : IRequest<IReadOnlyList<ResourceReservationResponse>>;
}
