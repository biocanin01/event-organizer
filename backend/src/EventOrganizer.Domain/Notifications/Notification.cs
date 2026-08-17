namespace EventOrganizer.Domain.Notifications
{
    public sealed class Notification
    {
        public const int MaxTitleLength = 200;
        public const int MaxMessageLength = 1000;

        private Notification() { }

        private Notification(
            Guid id,
            Guid recipientUserId,
            NotificationType type,
            string title,
            string message,
            DateTime createdAtUtc,
            NotificationRelatedEntityType? relatedEntityType,
            Guid? relatedEntityId)
        {
            Id = id;
            RecipientUserId = recipientUserId;
            Type = type;
            Title = title;
            Message = message;
            CreatedAtUtc = createdAtUtc;
            RelatedEntityType = relatedEntityType;
            RelatedEntityId = relatedEntityId;
            Version = 1;
        }

        public Guid Id { get; private set; }

        public Guid RecipientUserId { get; private set; }

        public NotificationType Type { get; private set; }

        public string Title { get; private set; } = string.Empty;

        public string Message { get; private set; } = string.Empty;

        public NotificationRelatedEntityType? RelatedEntityType { get; private set; }

        public Guid? RelatedEntityId { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime? ReadAtUtc { get; private set; }

        public bool IsRead => ReadAtUtc.HasValue;

        public int Version { get; private set; }

        public static Notification Create(
            Guid recipientUserId,
            NotificationType type,
            string title,
            string message,
            DateTime createdAtUtc,
            NotificationRelatedEntityType? relatedEntityType = null,
            Guid? relatedEntityId = null)
        {
            if (recipientUserId == Guid.Empty)
            {
                throw new ArgumentException("Recipient user id is required.", nameof(recipientUserId));
            }

            if (!Enum.IsDefined(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Notification type is invalid.");
            }

            var normalizedTitle = NormalizeRequiredText(
                title,
                MaxTitleLength,
                nameof(title),
                "Notification title");
            var normalizedMessage = NormalizeRequiredText(
                message,
                MaxMessageLength,
                nameof(message),
                "Notification message");

            ValidateRelatedEntity(relatedEntityType, relatedEntityId);

            return new Notification(
                Guid.NewGuid(),
                recipientUserId,
                type,
                normalizedTitle,
                normalizedMessage,
                createdAtUtc,
                relatedEntityType,
                relatedEntityId);
        }

        public void MarkAsRead(DateTime readAtUtc)
        {
            if (IsRead)
            {
                return;
            }

            if (readAtUtc < CreatedAtUtc)
            {
                throw new ArgumentException(
                    "The read time cannot be before the notification creation time.",
                    nameof(readAtUtc));
            }

            ReadAtUtc = readAtUtc;
            Version++;
        }

        private static string NormalizeRequiredText(
            string value,
            int maxLength,
            string parameterName,
            string displayName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{displayName} is required.", parameterName);
            }

            var normalizedValue = value.Trim();
            if (normalizedValue.Length > maxLength)
            {
                throw new ArgumentException(
                    $"{displayName} cannot exceed {maxLength} characters.",
                    parameterName);
            }

            return normalizedValue;
        }

        private static void ValidateRelatedEntity(
            NotificationRelatedEntityType? relatedEntityType,
            Guid? relatedEntityId)
        {
            if (relatedEntityType.HasValue != relatedEntityId.HasValue)
            {
                throw new ArgumentException(
                    "Related entity type and id must either both be provided or both be omitted.");
            }

            if (relatedEntityType.HasValue && !Enum.IsDefined(relatedEntityType.Value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(relatedEntityType),
                    "Related entity type is invalid.");
            }

            if (relatedEntityId == Guid.Empty)
            {
                throw new ArgumentException("Related entity id cannot be empty.", nameof(relatedEntityId));
            }
        }
    }
}
