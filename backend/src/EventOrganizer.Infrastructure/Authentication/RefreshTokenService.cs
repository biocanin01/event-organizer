using EventOrganizer.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace EventOrganizer.Infrastructure.Authentication
{
    public sealed class RefreshTokenService : IRefreshTokenService
    {
        private const int TokenSizeInBytes = 64;

        private readonly JwtSettings _jwtSettings;

        public RefreshTokenService(IOptions<JwtSettings> jwtOptions)
        {
            _jwtSettings = jwtOptions.Value;
        }

        public RefreshTokenResult CreateRefreshToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(TokenSizeInBytes);
            var token = Base64UrlEncoder.Encode(tokenBytes);

            return new RefreshTokenResult(
                token,
                HashToken(token),
                DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));
        }

        public string HashToken(string token)
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(hashBytes);
        }
    }
}
