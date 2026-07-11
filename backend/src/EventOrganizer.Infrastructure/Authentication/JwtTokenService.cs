using EventOrganizer.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EventOrganizer.Infrastructure.Authentication
{
    public class JwtTokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;

        public JwtTokenService(IOptions<JwtSettings> jwtOptions)
        {
            _jwtSettings = jwtOptions.Value;
        }

        public AccessTokenResult CreateAccessToken(Guid userId, string email, string fullName, IReadOnlyCollection<string> roles)
        {
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Email, email),
                new(ClaimTypes.Name, fullName),
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_jwtSettings.SigningKey)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: signingCredentials);


            var serializedToken = new JwtSecurityTokenHandler().WriteToken(token);

            return new AccessTokenResult(serializedToken, expiresAtUtc);
        }
    }
}
