using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Resources;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.MarkResourceUnavailable
{
    public sealed class MarkResourceUnavailableCommandHandler
        : IRequestHandler<MarkResourceUnavailableCommand>
    {
        private readonly IApplicationDbContext _dbContext;

        public MarkResourceUnavailableCommandHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(
            MarkResourceUnavailableCommand request,
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

            resource.MarkUnavailable(DateTime.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
