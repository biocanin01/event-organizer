using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.ListEventInsights
{
    public sealed record ListEventInsightsQuery : IRequest<IReadOnlyList<EventInsightSummaryResponse>>;
}
