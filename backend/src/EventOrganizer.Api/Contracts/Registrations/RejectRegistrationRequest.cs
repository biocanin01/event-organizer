namespace EventOrganizer.Api.Contracts.Registrations
{
    public sealed record RejectRegistrationRequest(string Reason, int Version);
}
