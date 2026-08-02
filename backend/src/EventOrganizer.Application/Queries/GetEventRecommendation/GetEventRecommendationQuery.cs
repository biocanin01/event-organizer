using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.GetEventRecommendation
{
    public sealed record GetEventRecommendationQuery(Guid EventId)
        : IRequest<EventRecommendationResponse>;
}
