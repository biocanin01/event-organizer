namespace EventOrganizer.Domain.Registrations
{
    public sealed class Registration
    {
        private Registration() { }

        private Registration(
            Guid id,
            Guid eventId,
            Guid participantUserId,
            DateTime createdAtUtc)
        {
            Id = id;
            EventId = eventId;
            ParticipantUserId = participantUserId;
            Status = RegistrationStatus.Pending;
            CreatedAtUtc = createdAtUtc;
        }

        public Guid Id { get; private set; }

        public Guid EventId { get; private set; }

        public Guid ParticipantUserId { get; private set; }

        public RegistrationStatus Status { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime? UpdatedAtUtc { get; private set; }

        public static Registration Create(
            Guid eventId,
            Guid participantUserId,
            DateTime createdAtUtc)
        {
            if (eventId == Guid.Empty)
            {
                throw new ArgumentException("Event id is required.", nameof(eventId));
            }

            if (participantUserId == Guid.Empty)
            {
                throw new ArgumentException("Participant user id is required.", nameof(participantUserId));
            }

            return new Registration(
                Guid.NewGuid(),
                eventId,
                participantUserId,
                createdAtUtc);
        }

        public void Confirm(DateTime updatedAtUtc)
        {
            if (Status != RegistrationStatus.Pending)
            {
                throw new InvalidOperationException("Only pending registrations can be confirmed.");
            }

            Status = RegistrationStatus.Confirmed;
            UpdatedAtUtc = updatedAtUtc;
        }

        public void Reject(DateTime updatedAtUtc)
        {
            if (Status != RegistrationStatus.Pending)
            {
                throw new InvalidOperationException("Only pending registrations can be rejected.");
            }

            Status = RegistrationStatus.Rejected;
            UpdatedAtUtc = updatedAtUtc;
        }

        public void Cancel(DateTime updatedAtUtc)
        {
            if (Status is RegistrationStatus.Cancelled or RegistrationStatus.Rejected)
            {
                throw new InvalidOperationException("Cancelled or rejected registrations cannot be cancelled.");
            }

            Status = RegistrationStatus.Cancelled;
            UpdatedAtUtc = updatedAtUtc;
        }
    }
}
