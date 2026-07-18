using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.GetPublishedEventById
{
    public sealed record GetPublishedEventByIdQuery(Guid EventId) : IRequest<EventResponse>;
}
