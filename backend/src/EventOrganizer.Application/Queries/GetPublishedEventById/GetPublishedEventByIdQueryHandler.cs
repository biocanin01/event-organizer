using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Registrations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.GetPublishedEventById
{
    public sealed class GetPublishedEventByIdQueryHandler
        : IRequestHandler<GetPublishedEventByIdQuery, EventResponse>
    {
        private readonly IApplicationDbContext _dbContext;

        public GetPublishedEventByIdQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<EventResponse> Handle(
            GetPublishedEventByIdQuery request,
            CancellationToken cancellationToken)
        {
            var eventResponse = await _dbContext.Events
                .AsNoTracking()
                .Where(eventItem =>
                    eventItem.Id == request.EventId &&
                    eventItem.Status == EventStatus.Published)
                .Select(eventItem => new EventResponse(
                    eventItem.Id,
                    eventItem.Title,
                    eventItem.Description,
                    eventItem.StartsAtUtc,
                    eventItem.EndsAtUtc,
                    eventItem.Capacity,
                    _dbContext.Registrations.Count(registration =>
                        registration.EventId == eventItem.Id
                        && registration.Status == RegistrationStatus.Confirmed),
                    eventItem.Budget,
                    eventItem.Area,
                    eventItem.RequiredSpeakerCount,
                    eventItem.RequiresEquipment,
                    eventItem.OrganizerUserId,
                    eventItem.Status.ToString(),
                    eventItem.CreatedAtUtc,
                    eventItem.UpdatedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);

            return eventResponse
                ?? throw new NotFoundException("Published event", request.EventId);
        }
    }
}
