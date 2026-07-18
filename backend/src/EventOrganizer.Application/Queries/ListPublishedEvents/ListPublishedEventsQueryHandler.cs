using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Events;
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
            return await _dbContext.Events
                .AsNoTracking()
                .Where(eventItem => eventItem.Status == EventStatus.Published)
                .OrderBy(eventItem => eventItem.StartsAtUtc)
                .Select(eventItem => new EventResponse(
                    eventItem.Id,
                    eventItem.Title,
                    eventItem.Description,
                    eventItem.StartsAtUtc,
                    eventItem.EndsAtUtc,
                    eventItem.Capacity,
                    eventItem.OrganizerUserId,
                    eventItem.Status.ToString(),
                    eventItem.CreatedAtUtc,
                    eventItem.UpdatedAtUtc))
                .ToListAsync(cancellationToken);
        }
    }
}
