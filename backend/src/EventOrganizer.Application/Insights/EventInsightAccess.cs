using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Insights
{
    internal static class EventInsightAccess
    {
        public static IQueryable<Event> ScopeEvents(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
            {
                throw new UnauthorizedException("An authenticated user is required to view insights.");
            }

            var query = dbContext.Events
                .AsNoTracking()
                .AsQueryable();

            if (currentUserService.IsInRole(ApplicationRoles.Admin))
            {
                return query;
            }

            if (currentUserService.IsInRole(ApplicationRoles.Organizer))
            {
                var organizerUserId = currentUserService.UserId.Value;
                return query.Where(eventItem => eventItem.OrganizerUserId == organizerUserId);
            }

            throw new ForbiddenException("Only organizers and admins can view event insights.");
        }
    }
}
