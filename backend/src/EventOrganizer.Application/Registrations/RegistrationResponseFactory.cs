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
            var events = await dbContext.Events
                .AsNoTracking()
                .Where(eventItem => eventIds.Contains(eventItem.Id))
                .ToDictionaryAsync(
                    eventItem => eventItem.Id,
                    eventItem => new
                    {
                        eventItem.Title,
                        eventItem.StartsAtUtc,
                        eventItem.EndsAtUtc,
                        eventItem.Status,
                    },
                    cancellationToken);
            var users = await userManagementService.FindUserSummariesByIdsAsync(
                registrations.Select(registration => registration.ParticipantUserId).Distinct().ToArray(),
                cancellationToken);

            return registrations.Select(registration =>
            {
                users.TryGetValue(registration.ParticipantUserId, out var user);
                events.TryGetValue(registration.EventId, out var eventItem);
                return new RegistrationResponse(
                    registration.Id,
                    registration.EventId,
                    eventItem?.Title ?? string.Empty,
                    eventItem?.StartsAtUtc ?? default,
                    eventItem?.EndsAtUtc ?? default,
                    eventItem?.Status.ToString() ?? string.Empty,
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
