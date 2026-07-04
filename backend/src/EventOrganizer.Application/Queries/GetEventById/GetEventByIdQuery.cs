using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.GetEventById
{
    public sealed record GetEventByIdQuery(Guid EventId) : IRequest<EventResponse?>;
}
