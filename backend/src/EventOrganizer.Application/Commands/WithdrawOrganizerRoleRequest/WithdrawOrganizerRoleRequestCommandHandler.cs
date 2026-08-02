using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.WithdrawOrganizerRoleRequest
{
    public sealed class WithdrawOrganizerRoleRequestCommandHandler
        : IRequestHandler<WithdrawOrganizerRoleRequestCommand>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public WithdrawOrganizerRoleRequestCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task Handle(
            WithdrawOrganizerRoleRequestCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedException("User must be authenticated.");

            var organizerRoleRequest = await _dbContext.OrganizerRoleRequests
                .FirstOrDefaultAsync(
                    roleRequest => roleRequest.Id == request.RequestId,
                    cancellationToken);

            if (organizerRoleRequest is null)
            {
                throw new NotFoundException(nameof(OrganizerRoleRequest), request.RequestId);
            }

            if (organizerRoleRequest.UserId != userId)
            {
                throw new ForbiddenException("Only the request owner can withdraw an organizer role request.");
            }

            EnsureExpectedVersion(organizerRoleRequest.Version, request.Version);

            organizerRoleRequest.Withdraw(DateTime.UtcNow);

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
