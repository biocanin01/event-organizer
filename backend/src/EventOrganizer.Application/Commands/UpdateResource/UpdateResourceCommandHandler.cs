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

            if (resource.Type != request.Type)
            {
                throw new ConflictException("Resource type cannot be changed.");
            }

            var updatedAtUtc = DateTime.UtcNow;

            switch (resource)
            {
                case Venue venue:
                    venue.UpdateDetails(
                        request.Name,
                        request.Description,
                        request.Cost,
                        request.Capacity!.Value,
                        request.QualityScore,
                        updatedAtUtc);
                    break;

                case Speaker speaker:
                    speaker.UpdateDetails(
                        request.Name,
                        request.Description,
                        request.Cost,
                        request.ExpertiseArea!,
                        request.QualityScore,
                        updatedAtUtc);
                    break;

                case EquipmentPackage equipmentPackage:
                    equipmentPackage.UpdateDetails(
                        request.Name,
                        request.Description,
                        request.Cost,
                        request.ProviderName!,
                        request.SupportedCapacity!.Value,
                        request.ServiceArea!,
                        request.IncludesTechnicalSupport!.Value,
                        request.ContentsSummary!,
                        request.QualityScore,
                        updatedAtUtc);
                    break;

                default:
                    throw new InvalidOperationException("Unsupported resource type.");
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
