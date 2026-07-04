using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.ListEvents
{
    public sealed class ListEventsQueryHandler
        : IRequestHandler<ListEventsQuery, IReadOnlyList<EventResponse>>
    {
        private readonly IApplicationDbContext _dbContext;

        public ListEventsQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<EventResponse>> Handle(ListEventsQuery request, CancellationToken cancellationToken)
        {
            return await _dbContext.Events
                .AsNoTracking()
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
