using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.GetMyOrganizerRoleRequest
{
    public sealed class GetMyOrganizerRoleRequestQueryHandler
        : IRequestHandler<GetMyOrganizerRoleRequestQuery, OrganizerRoleRequestResponse?>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetMyOrganizerRoleRequestQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<OrganizerRoleRequestResponse?> Handle(
            GetMyOrganizerRoleRequestQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedException("User must be authenticated.");

            return await _dbContext.OrganizerRoleRequests
                .AsNoTracking()
                .Where(roleRequest => roleRequest.UserId == userId)
                .OrderByDescending(roleRequest => roleRequest.SubmittedAtUtc)
                .Select(roleRequest => new OrganizerRoleRequestResponse(
                    roleRequest.Id,
                    roleRequest.UserId,
                    roleRequest.Motivation,
                    roleRequest.Status.ToString(),
                    roleRequest.ReviewedByAdminUserId,
                    roleRequest.DecisionReason,
                    roleRequest.SubmittedAtUtc,
                    roleRequest.ReviewedAtUtc,
                    roleRequest.WithdrawnAtUtc,
                    roleRequest.Version))
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
