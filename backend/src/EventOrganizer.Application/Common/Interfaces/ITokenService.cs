namespace EventOrganizer.Application.Common.Interfaces
{
    public interface ITokenService
    {
        AccessTokenResult CreateAccessToken(
            Guid userId,
            string email,
            string fullName,
            IReadOnlyCollection<string> roles);
    }
}