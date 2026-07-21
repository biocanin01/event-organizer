using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.ListResources
{
    public sealed class ListResourcesQueryHandler
        : IRequestHandler<ListResourcesQuery, IReadOnlyList<ResourceResponse>>
    {
        private readonly IApplicationDbContext _dbContext;

        public ListResourcesQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<ResourceResponse>> Handle(
            ListResourcesQuery request,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Resources
                .AsNoTracking()
                .OrderBy(resource => resource.Name)
                .Select(resource => new ResourceResponse(
                    resource.Id,
                    resource.Name,
                    resource.Description,
                    resource.Type.ToString(),
                    resource.Status.ToString(),
                    resource.CreatedAtUtc,
                    resource.UpdatedAtUtc))
                .ToListAsync(cancellationToken);
        }
    }
}