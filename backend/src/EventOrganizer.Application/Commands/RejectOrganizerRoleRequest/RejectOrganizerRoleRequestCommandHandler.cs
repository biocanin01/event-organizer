using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.RejectOrganizerRoleRequest
{
    public sealed class RejectOrganizerRoleRequestCommandHandler
        : IRequestHandler<RejectOrganizerRoleRequestCommand>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public RejectOrganizerRoleRequestCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task Handle(
            RejectOrganizerRoleRequestCommand request,
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

            organizerRoleRequest.Reject(
                adminUserId,
                request.DecisionReason,
                DateTime.UtcNow);

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
