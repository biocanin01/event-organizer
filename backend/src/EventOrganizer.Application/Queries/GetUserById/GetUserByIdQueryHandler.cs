using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.GetUserById
{
    public sealed class GetUserByIdQueryHandler
        : IRequestHandler<GetUserByIdQuery, UserDetailsResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IUserManagementService _userManagementService;

        public GetUserByIdQueryHandler(
            IApplicationDbContext dbContext,
            IUserManagementService userManagementService)
        {
            _dbContext = dbContext;
            _userManagementService = userManagementService;
        }

        public async Task<UserDetailsResponse> Handle(
            GetUserByIdQuery request,
            CancellationToken cancellationToken)
        {
            var user = await _userManagementService.FindUserSummaryByIdAsync(
                request.UserId,
                cancellationToken);

            if (user is null)
            {
                throw new NotFoundException("User", request.UserId);
            }

            var createdEventCount = await _dbContext.Events
                .AsNoTracking()
                .CountAsync(
                    eventItem => eventItem.OrganizerUserId == request.UserId,
                    cancellationToken);

            return new UserDetailsResponse(
                user.UserId,
                user.FullName,
                user.Email,
                user.Status.ToString(),
                user.CreatedAtUtc,
                user.VerifiedAtUtc,
                user.Roles,
                createdEventCount);
        }
    }
}
