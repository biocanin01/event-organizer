namespace EventOrganizer.Application.Common.Interfaces
{
    public sealed record AccessTokenResult(
        string Token,
        DateTime ExpiresAtUtc);
}