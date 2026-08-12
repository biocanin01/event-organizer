using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Domain.Bookings
{
    public sealed class EventResourceBooking
    {
        private readonly List<EventResourceBookingItem> _items = [];

        private EventResourceBooking() { }

        private EventResourceBooking(
            Guid id,
            Guid eventId,
            DateTime createdAtUtc)
        {
            Id = id;
            EventId = eventId;
            Status = EventResourceBookingStatus.Draft;
            Version = 1;
            CreatedAtUtc = createdAtUtc;
        }

        public Guid Id { get; private set; }

        public Guid EventId { get; private set; }

        public EventResourceBookingStatus Status { get; private set; }

        public int Version { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime? UpdatedAtUtc { get; private set; }

        public DateTime? SubmittedAtUtc { get; private set; }

        public DateTime? HoldExpiresAtUtc { get; private set; }

        public string? DecisionReason { get; private set; }

        public DateTime? DecidedAtUtc { get; private set; }

        public Guid? DecidedByUserId { get; private set; }

        public IReadOnlyCollection<EventResourceBookingItem> Items => _items.AsReadOnly();

        public static EventResourceBooking Create(
            Guid eventId,
            DateTime createdAtUtc)
        {
            if (eventId == Guid.Empty)
            {
                throw new ArgumentException("Event id is required.", nameof(eventId));
            }

            return new EventResourceBooking(
                Guid.NewGuid(),
                eventId,
                createdAtUtc);
        }

        public void AddResource(
            Guid resourceId,
            ResourceType resourceType,
            DateTime updatedAtUtc)
        {
            EnsureDraft();

            if (resourceId == Guid.Empty)
            {
                throw new ArgumentException("Resource id is required.", nameof(resourceId));
            }

            if (!Enum.IsDefined(resourceType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resourceType),
                    "Unsupported resource type.");
            }

            if (_items.Any(item => item.ResourceId == resourceId))
            {
                throw new InvalidOperationException("Resource is already part of this booking.");
            }

            if (resourceType == ResourceType.Venue &&
                _items.Any(item => item.ResourceType == ResourceType.Venue))
            {
                throw new InvalidOperationException("A booking can contain only one venue.");
            }

            if (resourceType == ResourceType.EquipmentPackage &&
                _items.Any(item => item.ResourceType == ResourceType.EquipmentPackage))
            {
                throw new InvalidOperationException(
                    "A booking can contain only one equipment package.");
            }

            _items.Add(EventResourceBookingItem.Create(Id, resourceId, resourceType));
            Touch(updatedAtUtc);
        }

        public void RemoveResource(Guid resourceId, DateTime updatedAtUtc)
        {
            EnsureDraft();

            if (resourceId == Guid.Empty)
            {
                throw new ArgumentException("Resource id is required.", nameof(resourceId));
            }

            var item = _items.FirstOrDefault(item => item.ResourceId == resourceId);
            if (item is null)
            {
                throw new InvalidOperationException("Resource is not part of this booking.");
            }

            _items.Remove(item);
            Touch(updatedAtUtc);
        }

        public void Cancel(DateTime updatedAtUtc)
        {
            if (Status is not (EventResourceBookingStatus.Draft
                or EventResourceBookingStatus.Submitted
                or EventResourceBookingStatus.Approved))
            {
                throw new InvalidOperationException("Only active bookings can be cancelled.");
            }

            Status = EventResourceBookingStatus.Cancelled;
            SubmittedAtUtc = null;
            HoldExpiresAtUtc = null;
            Touch(updatedAtUtc);
        }

        public void ReplaceResources(
            Guid? venueId,
            IReadOnlyCollection<Guid> speakerIds,
            Guid? equipmentPackageId,
            DateTime updatedAtUtc)
        {
            EnsureDraft();
            ArgumentNullException.ThrowIfNull(speakerIds);

            var newItems = new List<EventResourceBookingItem>();

            if (venueId.HasValue)
            {
                ValidateResourceId(venueId.Value);
                newItems.Add(EventResourceBookingItem.Create(Id, venueId.Value, ResourceType.Venue));
            }

            foreach (var speakerId in speakerIds)
            {
                ValidateResourceId(speakerId);
                if (newItems.Any(item => item.ResourceId == speakerId))
                {
                    throw new InvalidOperationException("Resource is already part of this booking.");
                }

                newItems.Add(EventResourceBookingItem.Create(Id, speakerId, ResourceType.Speaker));
            }

            if (equipmentPackageId.HasValue)
            {
                ValidateResourceId(equipmentPackageId.Value);
                if (newItems.Any(item => item.ResourceId == equipmentPackageId.Value))
                {
                    throw new InvalidOperationException("Resource is already part of this booking.");
                }

                newItems.Add(EventResourceBookingItem.Create(
                    Id,
                    equipmentPackageId.Value,
                    ResourceType.EquipmentPackage));
            }

            if (_items.Count == newItems.Count
                && _items
                    .OrderBy(item => item.ResourceId)
                    .Select(item => (item.ResourceId, item.ResourceType))
                    .SequenceEqual(newItems
                        .OrderBy(item => item.ResourceId)
                        .Select(item => (item.ResourceId, item.ResourceType))))
            {
                return;
            }

            _items.Clear();
            _items.AddRange(newItems);
            Touch(updatedAtUtc);
        }

        public void Submit(
            DateTime submittedAtUtc,
            DateTime holdExpiresAtUtc)
        {
            EnsureDraft();

            if (holdExpiresAtUtc <= submittedAtUtc)
            {
                throw new ArgumentException(
                    "Hold expiration must be after submission time.",
                    nameof(holdExpiresAtUtc));
            }

            Status = EventResourceBookingStatus.Submitted;
            SubmittedAtUtc = submittedAtUtc;
            HoldExpiresAtUtc = holdExpiresAtUtc;
            ClearDecision();
            Touch(submittedAtUtc);
        }

        public void Withdraw(DateTime updatedAtUtc)
        {
            if (Status != EventResourceBookingStatus.Submitted)
            {
                throw new InvalidOperationException("Only submitted bookings can be withdrawn.");
            }

            Status = EventResourceBookingStatus.Draft;
            SubmittedAtUtc = null;
            HoldExpiresAtUtc = null;
            Touch(updatedAtUtc);
        }

        public void Revise(DateTime updatedAtUtc)
        {
            if (Status is not (EventResourceBookingStatus.Rejected
                or EventResourceBookingStatus.Expired))
            {
                throw new InvalidOperationException(
                    "Only rejected or expired bookings can be revised.");
            }

            Status = EventResourceBookingStatus.Draft;
            SubmittedAtUtc = null;
            HoldExpiresAtUtc = null;
            ClearDecision();
            Touch(updatedAtUtc);
        }

        public void Approve(Guid adminUserId, DateTime decidedAtUtc)
        {
            EnsureSubmitted();
            ValidateAdminUserId(adminUserId);

            if (HoldExpiresAtUtc is null || HoldExpiresAtUtc <= decidedAtUtc)
            {
                throw new InvalidOperationException(
                    "Only submitted bookings with an active hold can be approved.");
            }

            Status = EventResourceBookingStatus.Approved;
            DecisionReason = null;
            DecidedAtUtc = decidedAtUtc;
            DecidedByUserId = adminUserId;
            Touch(decidedAtUtc);
        }

        public void Reject(Guid adminUserId, string? decisionReason, DateTime decidedAtUtc)
        {
            EnsureSubmitted();
            ValidateAdminUserId(adminUserId);

            Status = EventResourceBookingStatus.Rejected;
            DecisionReason = string.IsNullOrWhiteSpace(decisionReason)
                ? null
                : decisionReason.Trim();
            DecidedAtUtc = decidedAtUtc;
            DecidedByUserId = adminUserId;
            Touch(decidedAtUtc);
        }

        public bool Expire(DateTime updatedAtUtc)
        {
            if (Status != EventResourceBookingStatus.Submitted)
            {
                return false;
            }

            if (HoldExpiresAtUtc is null || HoldExpiresAtUtc > updatedAtUtc)
            {
                return false;
            }

            Status = EventResourceBookingStatus.Expired;
            Touch(updatedAtUtc);
            return true;
        }

        private void EnsureDraft()
        {
            if (Status != EventResourceBookingStatus.Draft)
            {
                throw new InvalidOperationException("Only draft bookings can be changed.");
            }
        }

        private void EnsureSubmitted()
        {
            if (Status != EventResourceBookingStatus.Submitted)
            {
                throw new InvalidOperationException("Only submitted bookings can be decided.");
            }
        }

        private void Touch(DateTime updatedAtUtc)
        {
            UpdatedAtUtc = updatedAtUtc;
            Version++;
        }

        private void ClearDecision()
        {
            DecisionReason = null;
            DecidedAtUtc = null;
            DecidedByUserId = null;
        }

        private static void ValidateResourceId(Guid resourceId)
        {
            if (resourceId == Guid.Empty)
            {
                throw new ArgumentException("Resource id is required.", nameof(resourceId));
            }
        }

        private static void ValidateAdminUserId(Guid adminUserId)
        {
            if (adminUserId == Guid.Empty)
            {
                throw new ArgumentException("Admin user id is required.", nameof(adminUserId));
            }
        }
    }
}
