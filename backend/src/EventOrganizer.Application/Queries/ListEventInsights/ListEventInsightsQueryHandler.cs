using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Insights;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Registrations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.ListEventInsights
{
    public sealed class ListEventInsightsQueryHandler
        : IRequestHandler<ListEventInsightsQuery, IReadOnlyList<EventInsightSummaryResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public ListEventInsightsQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<IReadOnlyList<EventInsightSummaryResponse>> Handle(
            ListEventInsightsQuery request,
            CancellationToken cancellationToken)
        {
            var events = await EventInsightAccess
                .ScopeEvents(_dbContext, _currentUserService)
                .OrderByDescending(eventItem => eventItem.StartsAtUtc)
                .ToArrayAsync(cancellationToken);

            if (events.Length == 0)
            {
                return Array.Empty<EventInsightSummaryResponse>();
            }

            var eventIds = events.Select(eventItem => eventItem.Id).ToArray();
            var registrationCounts = await _dbContext.Registrations
                .AsNoTracking()
                .Where(registration => eventIds.Contains(registration.EventId))
                .GroupBy(registration => new
                {
                    registration.EventId,
                    registration.Status,
                })
                .Select(group => new
                {
                    group.Key.EventId,
                    group.Key.Status,
                    Count = group.Count(),
                })
                .ToArrayAsync(cancellationToken);
            var reviewStats = await _dbContext.Reviews
                .AsNoTracking()
                .Where(review => eventIds.Contains(review.EventId))
                .GroupBy(review => review.EventId)
                .Select(group => new
                {
                    EventId = group.Key,
                    Count = group.Count(),
                    AverageRating = group.Average(review => review.Rating),
                })
                .ToDictionaryAsync(
                    item => item.EventId,
                    item => item,
                    cancellationToken);

            var registrationsByEvent = registrationCounts
                .GroupBy(item => item.EventId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyDictionary<RegistrationStatus, int>)group.ToDictionary(
                        item => item.Status,
                        item => item.Count));

            return events
                .Select(eventItem =>
                {
                    registrationsByEvent.TryGetValue(eventItem.Id, out var counts);
                    reviewStats.TryGetValue(eventItem.Id, out var stats);

                    return EventInsightProjection.CreateSummary(
                        eventItem,
                        counts ?? new Dictionary<RegistrationStatus, int>(),
                        stats?.Count ?? 0,
                        stats?.AverageRating);
                })
                .ToArray();
        }
    }
}
