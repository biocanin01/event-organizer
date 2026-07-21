using EventOrganizer.Application.Common.Interfaces;
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
            return await _dbContext.Resources
                .AsNoTracking()
                .Where(resource => resource.Id == request.ResourceId)
                .Select(resource => new ResourceResponse(
                    resource.Id,
                    resource.Name,
                    resource.Description,
                    resource.Type.ToString(),
                    resource.Status.ToString(),
                    resource.CreatedAtUtc,
                    resource.UpdatedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}