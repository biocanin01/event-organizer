using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Registrations;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.ListEventRegistrations
{
    public sealed class ListEventRegistrationsQueryHandler
        : IRequestHandler<ListEventRegistrationsQuery, IReadOnlyList<RegistrationResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IUserManagementService _userManagementService;
        private readonly EventAuthorizationService _eventAuthorizationService;

        public ListEventRegistrationsQueryHandler(
            IApplicationDbContext dbContext,
            IUserManagementService userManagementService,
            EventAuthorizationService eventAuthorizationService)
        {
            _dbContext = dbContext;
            _userManagementService = userManagementService;
            _eventAuthorizationService = eventAuthorizationService;
        }

        public async Task<IReadOnlyList<RegistrationResponse>> Handle(
            ListEventRegistrationsQuery request,
            CancellationToken cancellationToken)
        {
            var eventItem = await _dbContext.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(eventItem => eventItem.Id == request.EventId, cancellationToken);

            if (eventItem is null)
            {
                throw new NotFoundException(nameof(Event), request.EventId);
            }

            _eventAuthorizationService.EnsureCanManage(eventItem);
            var query = _dbContext.Registrations
                .AsNoTracking()
                .Where(registration => registration.EventId == request.EventId);

            if (request.Status.HasValue)
            {
                query = query.Where(registration => registration.Status == request.Status.Value);
            }

            var registrations = await query
                .OrderBy(registration => registration.CreatedAtUtc)
                .ToArrayAsync(cancellationToken);

            return await RegistrationResponseFactory.CreateManyAsync(
                _dbContext,
                _userManagementService,
                registrations,
                cancellationToken);
        }
    }
}
