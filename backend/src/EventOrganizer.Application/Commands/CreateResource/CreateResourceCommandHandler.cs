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
            var createdAtUtc = DateTime.UtcNow;
            Resource resource = request.Type switch
            {
                ResourceType.Venue => Venue.Create(
                    request.Name,
                    request.Description,
                    request.Cost,
                    request.Capacity!.Value,
                    request.QualityScore,
                    createdAtUtc),

                ResourceType.Speaker => Speaker.Create(
                    request.Name,
                    request.Description,
                    request.Cost,
                    request.ExpertiseArea!,
                    request.QualityScore,
                    createdAtUtc),

                ResourceType.EquipmentPackage => EquipmentPackage.Create(
                    request.Name,
                    request.Description,
                    request.Cost,
                    request.ProviderName!,
                    request.SupportedCapacity!.Value,
                    request.ServiceArea!,
                    request.IncludesTechnicalSupport!.Value,
                    request.ContentsSummary!,
                    request.QualityScore,
                    createdAtUtc),

                _ => throw new InvalidOperationException("Unsupported resource type."),
            };

            _dbContext.Resources.Add(resource);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return resource.Id;
        }
    }
}
