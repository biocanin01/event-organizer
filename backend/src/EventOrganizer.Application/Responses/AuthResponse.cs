namespace EventOrganizer.Application.Responses
{
    public sealed record AuthResponse(
        Guid UserId,
        string FullName,
        string Email,
        IReadOnlyCollection<string> Roles,
        string AccessToken,
        DateTime AccessTokenExpiresAtUtc,
        string? RefreshToken = null,
        DateTime? RefreshTokenExpiresAtUtc = null);
}
