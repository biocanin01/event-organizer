using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Common.Mapping;
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
            var resources = await _dbContext.Resources
                .AsNoTracking()
                .OrderBy(resource => resource.Name)
                .ToListAsync(cancellationToken);

            return resources
                .Select(ResourceResponseMapper.ToResponse)
                .ToArray();
        }
    }
}
