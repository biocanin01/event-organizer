namespace EventOrganizer.Application.Common.Options
{
    public sealed class BookingOptions
    {
        public const string SectionName = "Booking";

        public int HoldDurationHours { get; set; } = 48;
    }
}
