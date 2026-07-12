namespace EventOrganizer.Infrastructure.Identity
{
    public sealed class RefreshToken
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? RevokedAtUtc { get; set; }

        public string? ReplacedByTokenHash { get; set; }

        public string? CreatedByIpAddress { get; set; }

        public string? RevokedByIpAddress { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

        public bool IsRevoked => RevokedAtUtc is not null;

        public bool IsActive => !IsExpired && !IsRevoked;
    }
}
