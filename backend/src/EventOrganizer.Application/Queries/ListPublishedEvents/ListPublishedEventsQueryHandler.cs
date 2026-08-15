using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Registrations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.ListPublishedEvents
{
    public sealed class ListPublishedEventsQueryHandler
        : IRequestHandler<ListPublishedEventsQuery, IReadOnlyList<EventResponse>>
    {
        private readonly IApplicationDbContext _dbContext;

        public ListPublishedEventsQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<EventResponse>> Handle(
            ListPublishedEventsQuery request,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            return await _dbContext.Events
                .AsNoTracking()
                .Where(eventItem =>
                    eventItem.Status == EventStatus.Published
                    && eventItem.StartsAtUtc > now)
                .OrderBy(eventItem => eventItem.StartsAtUtc)
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
                .ToListAsync(cancellationToken);
        }
    }
}
