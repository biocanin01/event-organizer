using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Notifications;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.ListMyNotifications
{
    public sealed class ListMyNotificationsQueryHandler
        : IRequestHandler<ListMyNotificationsQuery, IReadOnlyList<NotificationResponse>>
    {
        public const int NotificationLimit = 50;

        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public ListMyNotificationsQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<IReadOnlyList<NotificationResponse>> Handle(
            ListMyNotificationsQuery request,
            CancellationToken cancellationToken)
        {
            var userId = NotificationGuards.RequireAuthenticatedUser(_currentUserService);
            var notifications = await _dbContext.Notifications
                .AsNoTracking()
                .Where(notification => notification.RecipientUserId == userId)
                .OrderByDescending(notification => notification.CreatedAtUtc)
                .Take(NotificationLimit)
                .ToArrayAsync(cancellationToken);

            return notifications
                .Select(NotificationResponseMapper.ToResponse)
                .ToArray();
        }
    }
}
