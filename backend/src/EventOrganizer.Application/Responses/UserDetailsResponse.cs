namespace EventOrganizer.Application.Responses
{
    public sealed record UserDetailsResponse(
        Guid Id,
        string FullName,
        string Email,
        string Status,
        DateTime CreatedAtUtc,
        DateTime? VerifiedAtUtc,
        IReadOnlyCollection<string> Roles,
        int CreatedEventCount);
}
