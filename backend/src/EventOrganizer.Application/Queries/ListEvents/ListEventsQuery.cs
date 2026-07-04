using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.ListEvents
{
    public sealed record ListEventsQuery : IRequest<IReadOnlyList<EventResponse>>;
}