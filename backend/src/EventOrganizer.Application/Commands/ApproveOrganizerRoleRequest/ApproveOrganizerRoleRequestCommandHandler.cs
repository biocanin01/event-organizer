using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Notifications;
using EventOrganizer.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.ApproveOrganizerRoleRequest
{
    public sealed class ApproveOrganizerRoleRequestCommandHandler
        : IRequestHandler<ApproveOrganizerRoleRequestCommand>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IIdentityService _identityService;
        private readonly INotificationService _notificationService;

        public ApproveOrganizerRoleRequestCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IIdentityService identityService,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _identityService = identityService;
            _notificationService = notificationService;
        }

        public async Task Handle(
            ApproveOrganizerRoleRequestCommand request,
            CancellationToken cancellationToken)
        {
            var adminUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException("User must be authenticated.");

            var organizerRoleRequest = await _dbContext.OrganizerRoleRequests
                .FirstOrDefaultAsync(
                    roleRequest => roleRequest.Id == request.RequestId,
                    cancellationToken);

            if (organizerRoleRequest is null)
            {
                throw new NotFoundException(nameof(OrganizerRoleRequest), request.RequestId);
            }

            EnsureExpectedVersion(organizerRoleRequest.Version, request.Version);

            var now = DateTime.UtcNow;
            organizerRoleRequest.Approve(adminUserId, now);
            _notificationService.AddOrganizerRoleRequestApproved(
                organizerRoleRequest.UserId,
                organizerRoleRequest.Id,
                now);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new ConflictException(
                    "Organizer role request was changed by another operation. Please reload it and try again.",
                    exception);
            }

            var roles = await _identityService.GetRolesAsync(
                organizerRoleRequest.UserId,
                cancellationToken);

            if (!roles.Contains(ApplicationRoles.Organizer))
            {
                await _identityService.AddToRoleAsync(
                    organizerRoleRequest.UserId,
                    ApplicationRoles.Organizer,
                    cancellationToken);
            }
        }

        private static void EnsureExpectedVersion(int currentVersion, int expectedVersion)
        {
            if (currentVersion != expectedVersion)
            {
                throw new ConflictException(
                    "Organizer role request was changed by another operation. Please reload it and try again.");
            }
        }
    }
}
