namespace EventOrganizer.Domain.Users
{
    public sealed class OrganizerRoleRequest
    {
        public const int MaxMotivationLength = 1000;
        public const int MaxDecisionReasonLength = 500;

        private OrganizerRoleRequest() { }

        private OrganizerRoleRequest(
            Guid id,
            Guid userId,
            string motivation,
            DateTime submittedAtUtc)
        {
            Id = id;
            UserId = userId;
            Motivation = motivation;
            Status = OrganizerRoleRequestStatus.Pending;
            SubmittedAtUtc = submittedAtUtc;
            Version = 1;
        }

        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public string Motivation { get; private set; } = string.Empty;

        public OrganizerRoleRequestStatus Status { get; private set; }

        public Guid? ReviewedByAdminUserId { get; private set; }

        public string? DecisionReason { get; private set; }

        public DateTime SubmittedAtUtc { get; private set; }

        public DateTime? ReviewedAtUtc { get; private set; }

        public DateTime? WithdrawnAtUtc { get; private set; }

        public int Version { get; private set; }

        public static OrganizerRoleRequest Create(
            Guid userId,
            string motivation,
            DateTime submittedAtUtc)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User id is required.", nameof(userId));
            }

            ValidateMotivation(motivation);

            return new OrganizerRoleRequest(
                Guid.NewGuid(),
                userId,
                motivation.Trim(),
                submittedAtUtc);
        }

        public void Approve(Guid adminUserId, DateTime reviewedAtUtc)
        {
            EnsurePending();
            ValidateAdminUserId(adminUserId);
            ValidateTransitionTime(reviewedAtUtc, nameof(reviewedAtUtc));

            Status = OrganizerRoleRequestStatus.Approved;
            ReviewedByAdminUserId = adminUserId;
            ReviewedAtUtc = reviewedAtUtc;
            Version++;
        }

        public void Reject(
            Guid adminUserId,
            string decisionReason,
            DateTime reviewedAtUtc)
        {
            EnsurePending();
            ValidateAdminUserId(adminUserId);
            ValidateDecisionReason(decisionReason);
            ValidateTransitionTime(reviewedAtUtc, nameof(reviewedAtUtc));

            Status = OrganizerRoleRequestStatus.Rejected;
            ReviewedByAdminUserId = adminUserId;
            DecisionReason = decisionReason.Trim();
            ReviewedAtUtc = reviewedAtUtc;
            Version++;
        }

        public void Withdraw(DateTime withdrawnAtUtc)
        {
            EnsurePending();
            ValidateTransitionTime(withdrawnAtUtc, nameof(withdrawnAtUtc));

            Status = OrganizerRoleRequestStatus.Withdrawn;
            WithdrawnAtUtc = withdrawnAtUtc;
            Version++;
        }

        private void EnsurePending()
        {
            if (Status != OrganizerRoleRequestStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Only pending organizer role requests can be changed.");
            }
        }

        private void ValidateTransitionTime(DateTime transitionAtUtc, string parameterName)
        {
            if (transitionAtUtc < SubmittedAtUtc)
            {
                throw new ArgumentException(
                    "The transition time cannot be before the submission time.",
                    parameterName);
            }
        }

        private static void ValidateMotivation(string motivation)
        {
            if (string.IsNullOrWhiteSpace(motivation))
            {
                throw new ArgumentException(
                    "Organizer role request motivation is required.",
                    nameof(motivation));
            }

            if (motivation.Trim().Length > MaxMotivationLength)
            {
                throw new ArgumentException(
                    $"Organizer role request motivation cannot exceed {MaxMotivationLength} characters.",
                    nameof(motivation));
            }
        }

        private static void ValidateAdminUserId(Guid adminUserId)
        {
            if (adminUserId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Admin user id is required.",
                    nameof(adminUserId));
            }
        }

        private static void ValidateDecisionReason(string decisionReason)
        {
            if (string.IsNullOrWhiteSpace(decisionReason))
            {
                throw new ArgumentException(
                    "Decision reason is required when rejecting an organizer role request.",
                    nameof(decisionReason));
            }

            if (decisionReason.Trim().Length > MaxDecisionReasonLength)
            {
                throw new ArgumentException(
                    $"Decision reason cannot exceed {MaxDecisionReasonLength} characters.",
                    nameof(decisionReason));
            }
        }
    }
}
