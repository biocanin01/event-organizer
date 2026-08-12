namespace EventOrganizer.Api.Authorization
{
    public static class AuthorizationPolicies
    {
        public const string CanCreateEvents = "CanCreateEvents";

        public const string CanManageEvents = "CanManageEvents";

        public const string CanManageResources = "CanManageResources";

        public const string CanBrowseResources = "CanBrowseResources";

        public const string CanRequestOrganizerRole = "CanRequestOrganizerRole";

        public const string CanManageOrganizerRoleRequests = "CanManageOrganizerRoleRequests";

        public const string CanManageBookings = "CanManageBookings";

        public const string CanManageUsers = "CanManageUsers";
    }
}
