using Microsoft.AspNetCore.Http;

namespace EventOrganizer.Api.Auth
{
    public static class RefreshTokenCookie
    {
        public const string Name = "eventorganizer_refresh_token";

        public static CookieOptions CreateOptions(
            HttpRequest request,
            DateTime expiresAtUtc)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = expiresAtUtc,
                Path = "/api/auth",
            };
        }

        public static CookieOptions DeleteOptions(HttpRequest request)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/api/auth",
            };
        }
    }
}
