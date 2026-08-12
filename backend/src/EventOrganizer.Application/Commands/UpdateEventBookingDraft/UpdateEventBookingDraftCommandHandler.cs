using EventOrganizer.Application.Bookings;
using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Resources;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.UpdateEventBookingDraft
{
    public sealed class UpdateEventBookingDraftCommandHandler
        : IRequestHandler<UpdateEventBookingDraftCommand, EventResourceBookingResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly EventAuthorizationService _authorizationService;

        public UpdateEventBookingDraftCommandHandler(
            IApplicationDbContext dbContext,
            EventAuthorizationService authorizationService)
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
        }

        public async Task<EventResourceBookingResponse> Handle(
            UpdateEventBookingDraftCommand request,
            CancellationToken cancellationToken)
        {
            var eventItem = await _dbContext.Events
                .FirstOrDefaultAsync(
                    eventItem => eventItem.Id == request.EventId,
                    cancellationToken);

            if (eventItem is null)
            {
                throw new NotFoundException(nameof(Event), request.EventId);
            }

            _authorizationService.EnsureCanMutateBooking(eventItem);

            if (eventItem.Status != EventStatus.Draft)
            {
                throw new ConflictException("Only draft events can have draft booking changes.");
            }

            var booking = await _dbContext.EventResourceBookings
                .Include(booking => booking.Items)
                .FirstOrDefaultAsync(
                    booking => booking.EventId == request.EventId,
                    cancellationToken);

            if (booking is null)
            {
                throw new NotFoundException(nameof(EventResourceBooking), request.EventId);
            }

            EventBookingVersionGuard.EnsureExpectedVersion(booking, request.Version);

            var requestedResources = await LoadRequestedResourcesAsync(
                request,
                cancellationToken);

            EnsureRequestedResourceTypes(request, requestedResources);

            booking.ReplaceResources(
                request.VenueId,
                request.SpeakerIds,
                request.EquipmentPackageId,
                DateTime.UtcNow);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new ConflictException(
                    "The booking has changed. Refresh it and try again.",
                    exception);
            }

            return await EventBookingResponseFactory.CreateAsync(
                _dbContext,
                booking,
                cancellationToken);
        }

        private async Task<Resource[]> LoadRequestedResourcesAsync(
            UpdateEventBookingDraftCommand request,
            CancellationToken cancellationToken)
        {
            var resourceIds = new[]
                {
                    request.VenueId,
                    request.EquipmentPackageId,
                }
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Concat(request.SpeakerIds)
                .Distinct()
                .ToArray();

            return resourceIds.Length == 0
                ? []
                : await _dbContext.Resources
                    .Where(resource => resourceIds.Contains(resource.Id))
                    .ToArrayAsync(cancellationToken);
        }

        private static void EnsureRequestedResourceTypes(
            UpdateEventBookingDraftCommand request,
            IReadOnlyCollection<Resource> resources)
        {
            var resourceById = resources.ToDictionary(resource => resource.Id);

            EnsureResource(request.VenueId, ResourceType.Venue, resourceById);
            EnsureResource(
                request.EquipmentPackageId,
                ResourceType.EquipmentPackage,
                resourceById);

            foreach (var speakerId in request.SpeakerIds)
            {
                EnsureResource(speakerId, ResourceType.Speaker, resourceById);
            }
        }

        private static void EnsureResource(
            Guid? resourceId,
            ResourceType expectedType,
            IReadOnlyDictionary<Guid, Resource> resourceById)
        {
            if (!resourceId.HasValue)
            {
                return;
            }

            if (!resourceById.TryGetValue(resourceId.Value, out var resource))
            {
                throw new NotFoundException(nameof(Resource), resourceId.Value);
            }

            if (resource.Type != expectedType)
            {
                throw new ConflictException(
                    $"Resource '{resourceId.Value}' is not a {expectedType}.");
            }
        }
    }
}
