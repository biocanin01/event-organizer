using EventOrganizer.Application.Bookings;
using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Common.Options;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Resources;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;

namespace EventOrganizer.Application.Commands.SubmitEventBooking
{
    public sealed class SubmitEventBookingCommandHandler
        : IRequestHandler<SubmitEventBookingCommand, EventResourceBookingResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly EventAuthorizationService _authorizationService;
        private readonly BookingOptions _bookingOptions;

        public SubmitEventBookingCommandHandler(
            IApplicationDbContext dbContext,
            EventAuthorizationService authorizationService,
            IOptions<BookingOptions> bookingOptions)
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _bookingOptions = bookingOptions.Value;
        }

        public async Task<EventResourceBookingResponse> Handle(
            SubmitEventBookingCommand request,
            CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var eventItem = await _dbContext.Events
                .FirstOrDefaultAsync(
                    eventItem => eventItem.Id == request.EventId,
                    cancellationToken);

            if (eventItem is null)
            {
                throw new NotFoundException(nameof(Event), request.EventId);
            }

            _authorizationService.EnsureCanMutateBooking(eventItem);

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

            var now = DateTime.UtcNow;
            EnsureEventCanBeSubmitted(eventItem, now);

            var resources = await LoadSelectedResourcesAsync(booking, cancellationToken);
            BookingSubmissionValidator.Validate(eventItem, booking, resources);

            var conflicts = await BookingConflictDetector.FindAsync(
                _dbContext,
                eventItem,
                booking,
                now,
                cancellationToken);

            if (conflicts.Count > 0)
            {
                throw new BookingConflictException(
                    "One or more selected resources are already held for overlapping events.",
                    conflicts);
            }

            booking.Submit(
                now,
                now.AddHours(_bookingOptions.HoldDurationHours));

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                await _dbContext.CommitTransactionAsync(transaction, cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new ConflictException(
                    "The booking has changed. Refresh it and try again.",
                    exception);
            }
            catch (DbUpdateException exception)
            {
                throw new ConflictException(
                    "The booking could not be submitted because another change was saved first.",
                    exception);
            }

            return await EventBookingResponseFactory.CreateAsync(
                _dbContext,
                booking,
                cancellationToken);
        }

        private static void EnsureEventCanBeSubmitted(Event eventItem, DateTime now)
        {
            if (eventItem.Status != EventStatus.Draft)
            {
                throw new ConflictException("Only draft events can submit a booking.");
            }

            if (eventItem.StartsAtUtc <= now)
            {
                throw new ConflictException("Bookings cannot be submitted for events that have already started.");
            }
        }

        private async Task<Resource[]> LoadSelectedResourcesAsync(
            EventResourceBooking booking,
            CancellationToken cancellationToken)
        {
            var resourceIds = booking.Items
                .Select(item => item.ResourceId)
                .ToArray();

            return resourceIds.Length == 0
                ? []
                : await _dbContext.Resources
                    .Where(resource => resourceIds.Contains(resource.Id))
                    .ToArrayAsync(cancellationToken);
        }
    }
}
