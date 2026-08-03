using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Common.Users;
using EventOrganizer.Domain.Users;
using MediatR;

namespace EventOrganizer.Application.Commands.SuspendUser
{
    public sealed class SuspendUserCommandHandler
        : IRequestHandler<SuspendUserCommand>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IClientContextService _clientContextService;
        private readonly IRefreshTokenRevocationService _refreshTokenRevocationService;
        private readonly IUserManagementService _userManagementService;

        public SuspendUserCommandHandler(
            ICurrentUserService currentUserService,
            IClientContextService clientContextService,
            IRefreshTokenRevocationService refreshTokenRevocationService,
            IUserManagementService userManagementService)
        {
            _currentUserService = currentUserService;
            _clientContextService = clientContextService;
            _refreshTokenRevocationService = refreshTokenRevocationService;
            _userManagementService = userManagementService;
        }

        public async Task Handle(
            SuspendUserCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException("User must be authenticated.");

            var targetUser = await _userManagementService.FindUserSummaryByIdAsync(
                request.UserId,
                cancellationToken);

            if (targetUser is null)
            {
                throw new NotFoundException("User", request.UserId);
            }

            UserStatusManagementRules.EnsureCanChangeStatus(currentUserId, targetUser);

            if (targetUser.Status != UserStatus.Suspended)
            {
                await _userManagementService.UpdateUserStatusAsync(
                    request.UserId,
                    UserStatus.Suspended,
                    cancellationToken);
            }

            await _refreshTokenRevocationService.RevokeAllForUserAsync(
                request.UserId,
                _clientContextService.IpAddress,
                cancellationToken);
        }
    }
}
