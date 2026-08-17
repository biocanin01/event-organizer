using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.MarkAllNotificationsAsRead
{
    public sealed class MarkAllNotificationsAsReadCommandHandler
        : IRequestHandler<MarkAllNotificationsAsReadCommand>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public MarkAllNotificationsAsReadCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task Handle(
            MarkAllNotificationsAsReadCommand request,
            CancellationToken cancellationToken)
        {
            var userId = NotificationGuards.RequireAuthenticatedUser(_currentUserService);
            var notifications = await _dbContext.Notifications
                .Where(notification => notification.RecipientUserId == userId
                    && notification.ReadAtUtc == null)
                .ToArrayAsync(cancellationToken);

            if (notifications.Length == 0)
            {
                return;
            }

            var readAtUtc = DateTime.UtcNow;
            foreach (var notification in notifications)
            {
                notification.MarkAsRead(readAtUtc);
            }

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new ConflictException(
                    "One or more notifications have changed. Refresh and try again.",
                    exception);
            }
        }
    }
}
