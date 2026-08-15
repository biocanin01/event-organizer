using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Application.Reviews;
using EventOrganizer.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.ListManagedReviews
{
    public sealed class ListManagedReviewsQueryHandler
        : IRequestHandler<ListManagedReviewsQuery, IReadOnlyList<ReviewResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserManagementService _userManagementService;
        private readonly EventAuthorizationService _eventAuthorizationService;

        public ListManagedReviewsQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IUserManagementService userManagementService,
            EventAuthorizationService eventAuthorizationService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userManagementService = userManagementService;
            _eventAuthorizationService = eventAuthorizationService;
        }

        public async Task<IReadOnlyList<ReviewResponse>> Handle(
            ListManagedReviewsQuery request,
            CancellationToken cancellationToken)
        {
            ReviewGuards.RequireAuthenticatedUser(_currentUserService);
            if (request.EventId.HasValue)
            {
                var eventItem = await _dbContext.Events
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        eventItem => eventItem.Id == request.EventId.Value,
                        cancellationToken);

                if (eventItem is null)
                {
                    throw new NotFoundException(nameof(Event), request.EventId.Value);
                }

                _eventAuthorizationService.EnsureCanManage(eventItem);
            }
            else if (!_currentUserService.IsInRole(ApplicationRoles.Admin))
            {
                throw new ForbiddenException("Only administrators can list reviews across all events.");
            }

            var query =
                from review in _dbContext.Reviews.AsNoTracking()
                join eventItem in _dbContext.Events.AsNoTracking()
                    on review.EventId equals eventItem.Id
                select new
                {
                    Review = review,
                    Event = eventItem,
                };

            if (request.EventId.HasValue)
            {
                query = query.Where(row => row.Event.Id == request.EventId.Value);
            }

            if (_currentUserService.IsInRole(ApplicationRoles.Organizer) &&
                !_currentUserService.IsInRole(ApplicationRoles.Admin))
            {
                query = query.Where(row => row.Event.OrganizerUserId == _currentUserService.UserId!.Value);
            }

            var reviews = await query
                .OrderByDescending(row => row.Review.CreatedAtUtc)
                .Select(row => row.Review)
                .ToArrayAsync(cancellationToken);

            return await ReviewResponseFactory.CreateManyAsync(
                _dbContext,
                _userManagementService,
                reviews,
                cancellationToken);
        }
    }
}
