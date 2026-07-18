using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.ListPublishedEvents
{
    public sealed record ListPublishedEventsQuery : IRequest<IReadOnlyList<EventResponse>>;
}
