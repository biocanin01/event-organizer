using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Application.Reviews;
using EventOrganizer.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.ListEventReviews
{
    public sealed class ListEventReviewsQueryHandler
        : IRequestHandler<ListEventReviewsQuery, IReadOnlyList<ReviewResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IUserManagementService _userManagementService;

        public ListEventReviewsQueryHandler(
            IApplicationDbContext dbContext,
            IUserManagementService userManagementService)
        {
            _dbContext = dbContext;
            _userManagementService = userManagementService;
        }

        public async Task<IReadOnlyList<ReviewResponse>> Handle(
            ListEventReviewsQuery request,
            CancellationToken cancellationToken)
        {
            var eventExists = await _dbContext.Events
                .AsNoTracking()
                .AnyAsync(eventItem => eventItem.Id == request.EventId, cancellationToken);

            if (!eventExists)
            {
                throw new NotFoundException(nameof(Event), request.EventId);
            }

            var reviews = await _dbContext.Reviews
                .AsNoTracking()
                .Where(review => review.EventId == request.EventId)
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
