using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.GetEventById
{
    public sealed class GetEventByIdQueryHandler
        : IRequestHandler<GetEventByIdQuery, EventResponse?>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly EventAuthorizationService _eventAuthorizationService;

        public GetEventByIdQueryHandler(
            IApplicationDbContext dbContext,
            EventAuthorizationService eventAuthorizationService)
        {
            _dbContext = dbContext;
            _eventAuthorizationService = eventAuthorizationService;
        }

        public async Task<EventResponse?> Handle(
            GetEventByIdQuery request,
            CancellationToken cancellationToken)
        {
            var eventItem = await _dbContext.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    eventItem => eventItem.Id == request.EventId,
                    cancellationToken);

            if (eventItem is null)
            {
                return null;
            }

            _eventAuthorizationService.EnsureCanManage(eventItem);

            return new EventResponse(
                eventItem.Id,
                eventItem.Title,
                eventItem.Description,
                eventItem.StartsAtUtc,
                eventItem.EndsAtUtc,
                eventItem.Capacity,
                eventItem.Budget,
                eventItem.Area,
                eventItem.RequiredSpeakerCount,
                eventItem.RequiresEquipment,
                eventItem.OrganizerUserId,
                eventItem.Status.ToString(),
                eventItem.CreatedAtUtc,
                eventItem.UpdatedAtUtc);
        }
    }
}
