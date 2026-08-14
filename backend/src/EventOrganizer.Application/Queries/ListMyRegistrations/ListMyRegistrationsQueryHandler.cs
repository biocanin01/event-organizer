using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Registrations;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.ListMyRegistrations
{
    public sealed class ListMyRegistrationsQueryHandler
        : IRequestHandler<ListMyRegistrationsQuery, IReadOnlyList<RegistrationResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserManagementService _userManagementService;

        public ListMyRegistrationsQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IUserManagementService userManagementService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userManagementService = userManagementService;
        }

        public async Task<IReadOnlyList<RegistrationResponse>> Handle(
            ListMyRegistrationsQuery request,
            CancellationToken cancellationToken)
        {
            var userId = RegistrationGuards.RequireAuthenticatedUser(_currentUserService);
            var registrations = await _dbContext.Registrations
                .AsNoTracking()
                .Where(registration => registration.ParticipantUserId == userId)
                .OrderByDescending(registration => registration.CreatedAtUtc)
                .ToArrayAsync(cancellationToken);

            return await RegistrationResponseFactory.CreateManyAsync(
                _dbContext,
                _userManagementService,
                registrations,
                cancellationToken);
        }
    }
}
