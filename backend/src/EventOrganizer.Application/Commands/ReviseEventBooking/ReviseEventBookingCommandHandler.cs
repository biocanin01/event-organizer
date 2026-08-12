using EventOrganizer.Application.Bookings;
using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.ReviseEventBooking
{
    public sealed class ReviseEventBookingCommandHandler
        : IRequestHandler<ReviseEventBookingCommand, EventResourceBookingResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly EventAuthorizationService _authorizationService;

        public ReviseEventBookingCommandHandler(
            IApplicationDbContext dbContext,
            EventAuthorizationService authorizationService)
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
        }

        public async Task<EventResourceBookingResponse> Handle(
            ReviseEventBookingCommand request,
            CancellationToken cancellationToken)
        {
            var (eventItem, booking) = await EventBookingLoader.LoadTrackedAsync(
                _dbContext,
                request.EventId,
                cancellationToken);

            _authorizationService.EnsureCanMutateBooking(eventItem);
            EventBookingVersionGuard.EnsureExpectedVersion(booking, request.Version);

            booking.Revise(DateTime.UtcNow);

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

    }
}
