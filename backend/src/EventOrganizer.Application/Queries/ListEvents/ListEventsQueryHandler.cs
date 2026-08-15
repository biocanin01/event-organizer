using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Registrations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.ListEvents
{
    public sealed class ListEventsQueryHandler
        : IRequestHandler<ListEventsQuery, IReadOnlyList<EventResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public ListEventsQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<IReadOnlyList<EventResponse>> Handle(
            ListEventsQuery request,
            CancellationToken cancellationToken)
        {
            if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            {
                throw new UnauthorizedException(
                    "An authenticated user is required to list manageable events.");
            }

            var query = _dbContext.Events
                .AsNoTracking()
                .AsQueryable();

            if (!_currentUserService.IsInRole(ApplicationRoles.Admin))
            {
                if (!_currentUserService.IsInRole(ApplicationRoles.Organizer))
                {
                    throw new ForbiddenException(
                        "Only organizers and admins can list manageable events.");
                }

                var organizerUserId = _currentUserService.UserId.Value;
                query = query.Where(eventItem => eventItem.OrganizerUserId == organizerUserId);
            }

            return await query
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
