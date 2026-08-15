using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Registrations;

namespace EventOrganizer.Application.Insights
{
    internal static class EventInsightProjection
    {
        public static EventInsightSummaryResponse CreateSummary(
            Event eventItem,
            IReadOnlyDictionary<RegistrationStatus, int> registrationCounts,
            int reviewCount,
            double? averageRating)
        {
            var confirmedCount = GetRegistrationCount(
                registrationCounts,
                RegistrationStatus.Confirmed);

            return new EventInsightSummaryResponse(
                eventItem.Id,
                eventItem.Title,
                eventItem.Status.ToString(),
                eventItem.StartsAtUtc,
                eventItem.EndsAtUtc,
                eventItem.Capacity,
                GetRegistrationCount(registrationCounts, RegistrationStatus.Pending),
                confirmedCount,
                GetRegistrationCount(registrationCounts, RegistrationStatus.Rejected),
                GetRegistrationCount(registrationCounts, RegistrationStatus.Cancelled),
                CalculateCapacityFillPercentage(confirmedCount, eventItem.Capacity),
                averageRating,
                reviewCount);
        }

        public static decimal CalculateCapacityFillPercentage(
            int confirmedRegistrationCount,
            int capacity)
        {
            return capacity <= 0
                ? 0
                : Math.Round(confirmedRegistrationCount * 100m / capacity, 2);
        }

        private static int GetRegistrationCount(
            IReadOnlyDictionary<RegistrationStatus, int> registrationCounts,
            RegistrationStatus status)
        {
            return registrationCounts.TryGetValue(status, out var count)
                ? count
                : 0;
        }
    }
}
