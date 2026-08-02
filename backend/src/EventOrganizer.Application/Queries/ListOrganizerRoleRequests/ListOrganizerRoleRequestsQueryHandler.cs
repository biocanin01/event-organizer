using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.ListOrganizerRoleRequests
{
    public sealed class ListOrganizerRoleRequestsQueryHandler
        : IRequestHandler<ListOrganizerRoleRequestsQuery, IReadOnlyList<OrganizerRoleRequestResponse>>
    {
        private readonly IApplicationDbContext _dbContext;

        public ListOrganizerRoleRequestsQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<OrganizerRoleRequestResponse>> Handle(
            ListOrganizerRoleRequestsQuery request,
            CancellationToken cancellationToken)
        {
            var status = request.Status ?? OrganizerRoleRequestStatus.Pending;

            return await _dbContext.OrganizerRoleRequests
                .AsNoTracking()
                .Where(roleRequest => roleRequest.Status == status)
                .OrderBy(roleRequest => roleRequest.SubmittedAtUtc)
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
                .ToListAsync(cancellationToken);
        }
    }
}
