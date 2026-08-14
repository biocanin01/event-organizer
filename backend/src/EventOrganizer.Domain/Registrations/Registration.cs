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
            Version = 1;
            CreatedAtUtc = createdAtUtc;
        }

        public Guid Id { get; private set; }

        public Guid EventId { get; private set; }

        public Guid ParticipantUserId { get; private set; }

        public RegistrationStatus Status { get; private set; }

        public string? RejectionReason { get; private set; }

        public DateTime? DecidedAtUtc { get; private set; }

        public Guid? DecidedByUserId { get; private set; }

        public int Version { get; private set; }

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

        public void Confirm(Guid decidedByUserId, DateTime updatedAtUtc)
        {
            if (Status != RegistrationStatus.Pending)
            {
                throw new InvalidOperationException("Only pending registrations can be confirmed.");
            }

            if (decidedByUserId == Guid.Empty)
            {
                throw new ArgumentException("Decision user id is required.", nameof(decidedByUserId));
            }

            Status = RegistrationStatus.Confirmed;
            DecidedAtUtc = updatedAtUtc;
            DecidedByUserId = decidedByUserId;
            UpdatedAtUtc = updatedAtUtc;
            Version++;
        }

        public void Reject(string reason, Guid decidedByUserId, DateTime updatedAtUtc)
        {
            if (Status != RegistrationStatus.Pending)
            {
                throw new InvalidOperationException("Only pending registrations can be rejected.");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Rejection reason is required.", nameof(reason));
            }

            if (reason.Trim().Length > 500)
            {
                throw new ArgumentException("Rejection reason cannot exceed 500 characters.", nameof(reason));
            }

            if (decidedByUserId == Guid.Empty)
            {
                throw new ArgumentException("Decision user id is required.", nameof(decidedByUserId));
            }

            Status = RegistrationStatus.Rejected;
            RejectionReason = reason.Trim();
            DecidedAtUtc = updatedAtUtc;
            DecidedByUserId = decidedByUserId;
            UpdatedAtUtc = updatedAtUtc;
            Version++;
        }

        public void Cancel(DateTime updatedAtUtc)
        {
            if (Status is RegistrationStatus.Cancelled or RegistrationStatus.Rejected)
            {
                throw new InvalidOperationException("Cancelled or rejected registrations cannot be cancelled.");
            }

            Status = RegistrationStatus.Cancelled;
            UpdatedAtUtc = updatedAtUtc;
            Version++;
        }
    }
}
