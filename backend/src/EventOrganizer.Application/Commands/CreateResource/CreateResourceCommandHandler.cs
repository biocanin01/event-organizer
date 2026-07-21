using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Resources;
using MediatR;

namespace EventOrganizer.Application.Commands.CreateResource
{
    public sealed class CreateResourceCommandHandler
        : IRequestHandler<CreateResourceCommand, Guid>
    {
        private readonly IApplicationDbContext _dbContext;

        public CreateResourceCommandHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Guid> Handle(
            CreateResourceCommand request,
            CancellationToken cancellationToken)
        {
            var resource = Resource.Create(
                request.Name,
                request.Description,
                request.Type,
                DateTime.UtcNow);

            _dbContext.Resources.Add(resource);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return resource.Id;
        }
    }
}
