using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Registrations;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Registrations
{
    internal static class RegistrationResponseFactory
    {
        public static async Task<IReadOnlyList<RegistrationResponse>> CreateManyAsync(
            IApplicationDbContext dbContext,
            IUserManagementService userManagementService,
            IReadOnlyCollection<Registration> registrations,
            CancellationToken cancellationToken)
        {
            var eventIds = registrations.Select(registration => registration.EventId).Distinct().ToArray();
            var eventTitles = await dbContext.Events
                .AsNoTracking()
                .Where(eventItem => eventIds.Contains(eventItem.Id))
                .ToDictionaryAsync(eventItem => eventItem.Id, eventItem => eventItem.Title, cancellationToken);
            var users = await userManagementService.FindUserSummariesByIdsAsync(
                registrations.Select(registration => registration.ParticipantUserId).Distinct().ToArray(),
                cancellationToken);

            return registrations.Select(registration =>
            {
                users.TryGetValue(registration.ParticipantUserId, out var user);
                return new RegistrationResponse(
                    registration.Id,
                    registration.EventId,
                    eventTitles.GetValueOrDefault(registration.EventId, string.Empty),
                    registration.ParticipantUserId,
                    user?.FullName ?? string.Empty,
                    user?.Email ?? string.Empty,
                    registration.Status.ToString(),
                    registration.RejectionReason,
                    registration.DecidedAtUtc,
                    registration.DecidedByUserId,
                    registration.Version,
                    registration.CreatedAtUtc,
                    registration.UpdatedAtUtc);
            }).ToArray();
        }

        public static async Task<RegistrationResponse> CreateAsync(
            IApplicationDbContext dbContext,
            IUserManagementService userManagementService,
            Registration registration,
            CancellationToken cancellationToken)
        {
            return (await CreateManyAsync(
                dbContext,
                userManagementService,
                [registration],
                cancellationToken))[0];
        }
    }
}
