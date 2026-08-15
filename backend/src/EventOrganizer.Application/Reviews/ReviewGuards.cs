using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Reviews;

namespace EventOrganizer.Application.Reviews
{
    internal static class ReviewGuards
    {
        public static Guid RequireAuthenticatedUser(ICurrentUserService currentUserService)
        {
            return currentUserService.IsAuthenticated && currentUserService.UserId.HasValue
                ? currentUserService.UserId.Value
                : throw new UnauthorizedException("An authenticated user is required.");
        }

        public static void EnsureOwner(Review review, Guid userId)
        {
            if (review.ParticipantUserId != userId)
            {
                throw new ForbiddenException("The current user cannot change this review.");
            }
        }

        public static void EnsureExpectedVersion(Review review, int version)
        {
            if (review.Version != version)
            {
                throw new ConflictException("The review has changed. Refresh it and try again.");
            }
        }
    }
}
