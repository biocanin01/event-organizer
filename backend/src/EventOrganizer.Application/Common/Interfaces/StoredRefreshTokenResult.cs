namespace EventOrganizer.Application.Common.Interfaces
{
    public sealed record StoredRefreshTokenResult(
        Guid Id,
        Guid UserId,
        string TokenHash,
        DateTime ExpiresAtUtc,
        DateTime? RevokedAtUtc)
    {
        public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

        public bool IsRevoked => RevokedAtUtc is not null;

        public bool IsActive => !IsExpired && !IsRevoked;
    }
}