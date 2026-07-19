namespace EventOrganizer.Infrastructure.Identity
{
    public sealed class InitialAdminSettings
    {
        public const string SectionName = "InitialAdmin";

        public string Email { get; init; } = string.Empty;

        public string Password { get; init; } = string.Empty;

        public string FullName { get; init; } = string.Empty;
    }
}