using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Reviews
{
    internal static class ReviewResponseFactory
    {
        public static async Task<ReviewResponse> CreateAsync(
            IApplicationDbContext dbContext,
            IUserManagementService userManagementService,
            Review review,
            CancellationToken cancellationToken)
        {
            var responses = await CreateManyAsync(
                dbContext,
                userManagementService,
                new[] { review },
                cancellationToken);

            return responses[0];
        }

        public static async Task<IReadOnlyList<ReviewResponse>> CreateManyAsync(
            IApplicationDbContext dbContext,
            IUserManagementService userManagementService,
            IReadOnlyCollection<Review> reviews,
            CancellationToken cancellationToken)
        {
            if (reviews.Count == 0)
            {
                return Array.Empty<ReviewResponse>();
            }

            var eventIds = reviews
                .Select(review => review.EventId)
                .Distinct()
                .ToArray();
            var participantUserIds = reviews
                .Select(review => review.ParticipantUserId)
                .Distinct()
                .ToArray();

            var eventTitles = await dbContext.Events
                .AsNoTracking()
                .Where(eventItem => eventIds.Contains(eventItem.Id))
                .ToDictionaryAsync(
                    eventItem => eventItem.Id,
                    eventItem => eventItem.Title,
                    cancellationToken);

            var users = await userManagementService.FindUserSummariesByIdsAsync(
                participantUserIds,
                cancellationToken);

            return reviews
                .Select(review => new ReviewResponse(
                    review.Id,
                    review.EventId,
                    eventTitles.GetValueOrDefault(review.EventId, string.Empty),
                    review.ParticipantUserId,
                    users.TryGetValue(review.ParticipantUserId, out var user) ? user.FullName : string.Empty,
                    review.Rating,
                    review.Comment,
                    review.Version,
                    review.CreatedAtUtc,
                    review.UpdatedAtUtc))
                .ToArray();
        }
    }
}
