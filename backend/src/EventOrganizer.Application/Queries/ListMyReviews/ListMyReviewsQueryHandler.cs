using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Application.Reviews;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.ListMyReviews
{
    public sealed class ListMyReviewsQueryHandler
        : IRequestHandler<ListMyReviewsQuery, IReadOnlyList<ReviewResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserManagementService _userManagementService;

        public ListMyReviewsQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IUserManagementService userManagementService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userManagementService = userManagementService;
        }

        public async Task<IReadOnlyList<ReviewResponse>> Handle(
            ListMyReviewsQuery request,
            CancellationToken cancellationToken)
        {
            var userId = ReviewGuards.RequireAuthenticatedUser(_currentUserService);
            var reviews = await _dbContext.Reviews
                .AsNoTracking()
                .Where(review => review.ParticipantUserId == userId)
                .OrderByDescending(review => review.CreatedAtUtc)
                .ToArrayAsync(cancellationToken);

            return await ReviewResponseFactory.CreateManyAsync(
                _dbContext,
                _userManagementService,
                reviews,
                cancellationToken);
        }
    }
}
