namespace EventOrganizer.Api.Authorization
{
    public static class AuthorizationPolicies
    {
        public const string CanCreateEvents = "CanCreateEvents";

        public const string CanManageEvents = "CanManageEvents";

        public const string CanManageResources = "CanManageResources";

        public const string CanCreateResourceReservations = "CanCreateResourceReservations";

        public const string CanManageResourceReservations = "CanManageResourceReservations";

        public const string CanCancelResourceReservations = "CanCancelResourceReservations";

        public const string CanRequestOrganizerRole = "CanRequestOrganizerRole";

        public const string CanManageOrganizerRoleRequests = "CanManageOrganizerRoleRequests";
    }
}
