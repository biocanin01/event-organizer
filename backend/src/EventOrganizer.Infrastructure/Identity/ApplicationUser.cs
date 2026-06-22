using EventOrganizer.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace EventOrganizer.Infrastructure.Identity
{
    public sealed class ApplicationUser : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;

        public UserStatus Status { get; set; } = UserStatus.PendingVerification;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? VerifiedAtUtc { get; set; }
    }
}
