using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Notifications;
using EventOrganizer.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.MarkNotificationAsRead
{
    public sealed class MarkNotificationAsReadCommandHandler
        : IRequestHandler<MarkNotificationAsReadCommand>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public MarkNotificationAsReadCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task Handle(
            MarkNotificationAsReadCommand request,
            CancellationToken cancellationToken)
        {
            var userId = NotificationGuards.RequireAuthenticatedUser(_currentUserService);
            var notification = await _dbContext.Notifications.FirstOrDefaultAsync(
                item => item.Id == request.NotificationId
                    && item.RecipientUserId == userId,
                cancellationToken);

            if (notification is null)
            {
                throw new NotFoundException(nameof(Notification), request.NotificationId);
            }

            if (notification.IsRead)
            {
                return;
            }

            notification.MarkAsRead(DateTime.UtcNow);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                var wasReadConcurrently = await _dbContext.Notifications
                    .AsNoTracking()
                    .AnyAsync(
                        item => item.Id == request.NotificationId
                            && item.RecipientUserId == userId
                            && item.ReadAtUtc != null,
                        cancellationToken);

                if (!wasReadConcurrently)
                {
                    throw new ConflictException(
                        "The notification has changed. Refresh and try again.",
                        exception);
                }
            }
        }
    }
}
