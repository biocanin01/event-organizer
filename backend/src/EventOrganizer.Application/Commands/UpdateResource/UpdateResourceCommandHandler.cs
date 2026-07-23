using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Resources;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.UpdateResource
{
    public sealed class UpdateResourceCommandHandler
        : IRequestHandler<UpdateResourceCommand>
    {
        private readonly IApplicationDbContext _dbContext;

        public UpdateResourceCommandHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(
            UpdateResourceCommand request,
            CancellationToken cancellationToken)
        {
            var resource = await _dbContext.Resources
                .FirstOrDefaultAsync(
                    resource => resource.Id == request.ResourceId,
                    cancellationToken);

            if (resource is null)
            {
                throw new NotFoundException(nameof(Resource), request.ResourceId);
            }

            resource.UpdateDetails(
                request.Name,
                request.Description,
                DateTime.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
