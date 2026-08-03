using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Common.Users;
using EventOrganizer.Domain.Users;
using MediatR;

namespace EventOrganizer.Application.Commands.ReactivateUser
{
    public sealed class ReactivateUserCommandHandler
        : IRequestHandler<ReactivateUserCommand>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserManagementService _userManagementService;

        public ReactivateUserCommandHandler(
            ICurrentUserService currentUserService,
            IUserManagementService userManagementService)
        {
            _currentUserService = currentUserService;
            _userManagementService = userManagementService;
        }

        public async Task Handle(
            ReactivateUserCommand request,
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

            if (targetUser.Status == UserStatus.Active)
            {
                return;
            }

            if (targetUser.Status != UserStatus.Suspended)
            {
                throw new ConflictException("Only suspended users can be reactivated.");
            }

            await _userManagementService.UpdateUserStatusAsync(
                request.UserId,
                UserStatus.Active,
                cancellationToken);
        }
    }
}
