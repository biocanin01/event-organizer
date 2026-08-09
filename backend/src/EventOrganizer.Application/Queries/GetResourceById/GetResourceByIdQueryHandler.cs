using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Common.Mapping;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.GetResourceById
{
    public sealed class GetResourceByIdQueryHandler
        : IRequestHandler<GetResourceByIdQuery, ResourceResponse?>
    {
        private readonly IApplicationDbContext _dbContext;

        public GetResourceByIdQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ResourceResponse?> Handle(
            GetResourceByIdQuery request,
            CancellationToken cancellationToken)
        {
            var resource = await _dbContext.Resources
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    resource => resource.Id == request.ResourceId,
                    cancellationToken);

            return resource is null
                ? null
                : ResourceResponseMapper.ToResponse(resource);
        }
    }
}
