using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Notifications;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.GetUnreadNotificationCount
{
    public sealed class GetUnreadNotificationCountQueryHandler
        : IRequestHandler<GetUnreadNotificationCountQuery, UnreadNotificationCountResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetUnreadNotificationCountQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<UnreadNotificationCountResponse> Handle(
            GetUnreadNotificationCountQuery request,
            CancellationToken cancellationToken)
        {
            var userId = NotificationGuards.RequireAuthenticatedUser(_currentUserService);
            var unreadCount = await _dbContext.Notifications.CountAsync(
                notification => notification.RecipientUserId == userId
                    && notification.ReadAtUtc == null,
                cancellationToken);

            return new UnreadNotificationCountResponse(unreadCount);
        }
    }
}
