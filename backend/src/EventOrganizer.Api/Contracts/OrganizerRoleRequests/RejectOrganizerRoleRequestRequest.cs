namespace EventOrganizer.Api.Contracts.OrganizerRoleRequests
{
    public sealed record RejectOrganizerRoleRequestRequest(
        string DecisionReason,
        int Version);
}
