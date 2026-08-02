using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.SubmitOrganizerRoleRequest
{
    public sealed class SubmitOrganizerRoleRequestCommandHandler
        : IRequestHandler<SubmitOrganizerRoleRequestCommand, Guid>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IIdentityService _identityService;

        public SubmitOrganizerRoleRequestCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IIdentityService identityService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _identityService = identityService;
        }

        public async Task<Guid> Handle(
            SubmitOrganizerRoleRequestCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedException("User must be authenticated.");

            var user = await _identityService.FindByIdAsync(userId, cancellationToken);

            if (user is null)
            {
                throw new UnauthorizedException("Authenticated user was not found.");
            }

            if (user.Status != UserStatus.Active)
            {
                throw new ForbiddenException("Only active users can request organizer privileges.");
            }

            var roles = await _identityService.GetRolesAsync(userId, cancellationToken);

            if (roles.Contains(ApplicationRoles.Organizer) ||
                roles.Contains(ApplicationRoles.Admin))
            {
                throw new ConflictException("User already has organizer privileges.");
            }

            var hasPendingRequest = await _dbContext.OrganizerRoleRequests
                .AnyAsync(
                    roleRequest =>
                        roleRequest.UserId == userId
                        && roleRequest.Status == OrganizerRoleRequestStatus.Pending,
                    cancellationToken);

            if (hasPendingRequest)
            {
                throw new ConflictException("User already has a pending organizer role request.");
            }

            var organizerRoleRequest = OrganizerRoleRequest.Create(
                userId,
                request.Motivation,
                DateTime.UtcNow);

            _dbContext.OrganizerRoleRequests.Add(organizerRoleRequest);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                throw new ConflictException(
                    "User already has a pending organizer role request.",
                    exception);
            }

            return organizerRoleRequest.Id;
        }
    }
}
