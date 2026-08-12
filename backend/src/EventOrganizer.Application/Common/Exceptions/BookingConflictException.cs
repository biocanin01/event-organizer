using EventOrganizer.Application.Common.Bookings;

namespace EventOrganizer.Application.Common.Exceptions
{
    public sealed class BookingConflictException : ConflictException
    {
        public BookingConflictException(
            string message,
            IReadOnlyList<BookingConflictDetail> conflicts)
            : base(message)
        {
            Conflicts = conflicts;
        }

        public IReadOnlyList<BookingConflictDetail> Conflicts { get; }
    }
}
