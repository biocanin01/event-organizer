namespace EventOrganizer.Api.Contracts.Events
{
    public sealed record CreateEventRequest(
        string Title,
        string Description,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc,
        int Capacity);
}