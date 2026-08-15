using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.GetEventInsightById
{
    public sealed record GetEventInsightByIdQuery(Guid EventId) : IRequest<EventInsightDetailsResponse>;
}
