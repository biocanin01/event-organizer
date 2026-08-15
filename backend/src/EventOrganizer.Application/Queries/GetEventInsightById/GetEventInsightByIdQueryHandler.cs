using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Insights;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Registrations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.GetEventInsightById
{
    public sealed class GetEventInsightByIdQueryHandler
        : IRequestHandler<GetEventInsightByIdQuery, EventInsightDetailsResponse>
    {
        private const int RecentReviewLimit = 5;

        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserManagementService _userManagementService;

        public GetEventInsightByIdQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IUserManagementService userManagementService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userManagementService = userManagementService;
        }

        public async Task<EventInsightDetailsResponse> Handle(
            GetEventInsightByIdQuery request,
            CancellationToken cancellationToken)
        {
            var eventItem = await EventInsightAccess
                .ScopeEvents(_dbContext, _currentUserService)
                .FirstOrDefaultAsync(
                    eventItem => eventItem.Id == request.EventId,
                    cancellationToken);

            if (eventItem is null)
            {
                throw new NotFoundException(nameof(Event), request.EventId);
            }

            var registrationCounts = await _dbContext.Registrations
                .AsNoTracking()
                .Where(registration => registration.EventId == eventItem.Id)
                .GroupBy(registration => registration.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count(),
                })
                .ToDictionaryAsync(
                    item => item.Status,
                    item => item.Count,
                    cancellationToken);

            var reviews = await _dbContext.Reviews
                .AsNoTracking()
                .Where(review => review.EventId == eventItem.Id)
                .ToArrayAsync(cancellationToken);
            var ratingCounts = reviews
                .GroupBy(review => review.Rating)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count());
            var recentReviews = reviews
                .OrderByDescending(review => review.UpdatedAtUtc ?? review.CreatedAtUtc)
                .Take(RecentReviewLimit)
                .ToArray();
            var participantUsers = await _userManagementService.FindUserSummariesByIdsAsync(
                recentReviews
                    .Select(review => review.ParticipantUserId)
                    .Distinct()
                    .ToArray(),
                cancellationToken);
            var summary = EventInsightProjection.CreateSummary(
                eventItem,
                registrationCounts,
                reviews.Length,
                reviews.Length == 0 ? null : reviews.Average(review => review.Rating));

            return new EventInsightDetailsResponse(
                summary.EventId,
                summary.EventTitle,
                summary.Status,
                summary.StartsAtUtc,
                summary.EndsAtUtc,
                summary.Capacity,
                summary.PendingRegistrationCount,
                summary.ConfirmedRegistrationCount,
                summary.RejectedRegistrationCount,
                summary.CancelledRegistrationCount,
                summary.CapacityFillPercentage,
                summary.AverageRating,
                summary.ReviewCount,
                Enumerable.Range(1, 5)
                    .Select(rating => new RatingDistributionResponse(
                        rating,
                        ratingCounts.GetValueOrDefault(rating)))
                    .ToArray(),
                recentReviews
                    .Select(review =>
                    {
                        participantUsers.TryGetValue(review.ParticipantUserId, out var user);

                        return new RecentReviewResponse(
                            review.Id,
                            review.ParticipantUserId,
                            user?.FullName ?? string.Empty,
                            review.Rating,
                            review.Comment,
                            review.CreatedAtUtc,
                            review.UpdatedAtUtc);
                    })
                    .ToArray());
        }
    }
}
