namespace EventOrganizer.Infrastructure.Persistance.Seeding
{
    public sealed class DemoDataSettings
    {
        public const string SectionName = "DemoData";

        public bool Enabled { get; init; }

        public string Password { get; init; } = string.Empty;
    }
}
