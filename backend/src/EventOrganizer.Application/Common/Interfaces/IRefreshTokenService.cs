namespace EventOrganizer.Application.Common.Interfaces
{
    public interface IRefreshTokenService
    {
        RefreshTokenResult CreateRefreshToken();

        string HashToken(string token);
    }
}
