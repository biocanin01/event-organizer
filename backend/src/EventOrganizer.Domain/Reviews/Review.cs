namespace EventOrganizer.Domain.Reviews
{
    public sealed class Review
    {
        private Review() { }

        private Review(
            Guid id,
            Guid eventId,
            Guid participantUserId,
            int rating,
            string comment,
            DateTime createdAtUtc)
        {
            Id = id;
            EventId = eventId;
            ParticipantUserId = participantUserId;
            Rating = rating;
            Comment = comment;
            Version = 1;
            CreatedAtUtc = createdAtUtc;
        }

        public Guid Id { get; private set; }

        public Guid EventId { get; private set; }

        public Guid ParticipantUserId { get; private set; }

        public int Rating { get; private set; }

        public string Comment { get; private set; } = string.Empty;

        public int Version { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime? UpdatedAtUtc { get; private set; }

        public static Review Create(
            Guid eventId,
            Guid participantUserId,
            int rating,
            string comment,
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

            ValidateRating(rating);
            var normalizedComment = NormalizeComment(comment);

            return new Review(
                Guid.NewGuid(),
                eventId,
                participantUserId,
                rating,
                normalizedComment,
                createdAtUtc);
        }

        public void Update(int rating, string comment, DateTime updatedAtUtc)
        {
            ValidateRating(rating);
            var normalizedComment = NormalizeComment(comment);

            Rating = rating;
            Comment = normalizedComment;
            UpdatedAtUtc = updatedAtUtc;
            Version++;
        }

        private static void ValidateRating(int rating)
        {
            if (rating is < 1 or > 5)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rating),
                    "Review rating must be between 1 and 5.");
            }
        }

        private static string NormalizeComment(string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                throw new ArgumentException("Review comment is required.", nameof(comment));
            }

            var normalizedComment = comment.Trim();
            if (normalizedComment.Length > 2000)
            {
                throw new ArgumentException("Review comment cannot exceed 2000 characters.", nameof(comment));
            }

            return normalizedComment;
        }
    }
}
