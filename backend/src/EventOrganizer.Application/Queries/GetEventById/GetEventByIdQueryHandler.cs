using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.GetEventById
{
    public sealed class GetEventByIdQueryHandler
        : IRequestHandler<GetEventByIdQuery, EventResponse?>
    {
        private readonly IApplicationDbContext _dbContext;

        public GetEventByIdQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<EventResponse?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
        {
            return await _dbContext.Events
                .AsNoTracking()
                .Where(eventItem => eventItem.Id == request.EventId)
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
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
