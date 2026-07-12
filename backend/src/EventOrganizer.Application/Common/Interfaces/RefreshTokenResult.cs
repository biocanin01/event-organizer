namespace EventOrganizer.Application.Common.Interfaces
{
    public sealed record RefreshTokenResult(
        string Token,
        string TokenHash,
        DateTime ExpiresAtUtc);
}
