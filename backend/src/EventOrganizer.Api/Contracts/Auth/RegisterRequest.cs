namespace EventOrganizer.Api.Contracts.Auth
{
    public sealed record RegisterRequest(
        string FullName,
        string Email,
        string Password);
}
