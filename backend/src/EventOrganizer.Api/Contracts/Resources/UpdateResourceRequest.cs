namespace EventOrganizer.Api.Contracts.Resources
{
    public sealed record UpdateResourceRequest(
        string Name,
        string Description);
}
