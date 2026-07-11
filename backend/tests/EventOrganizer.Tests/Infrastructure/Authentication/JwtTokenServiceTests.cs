using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EventOrganizer.Tests.Infrastructure.Authentication
{
    public sealed class JwtTokenServiceTests
    {
        [Fact]
        public void CreateAccessToken_WithUserData_ReturnsTokenWithExpectedClaims()
        {
            var settings = new JwtSettings
            {
                Issuer = "EventOrganizer",
                Audience = "EventOrganizerClient",
                SigningKey = "test-signing-key-for-jwt-token-service-tests-12345",
                AccessTokenExpirationMinutes = 15,
            };

            var tokenService = new JwtTokenService(Options.Create(settings));

            var userId = Guid.NewGuid();
            var roles = new[]
            {
                ApplicationRoles.Participant,
            };

            var result = tokenService.CreateAccessToken(
                userId,
                "test.user@example.com",
                "Test User",
                roles);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

            Assert.False(string.IsNullOrWhiteSpace(result.Token));
            Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
            Assert.Equal(settings.Issuer, jwt.Issuer);
            Assert.Contains(settings.Audience, jwt.Audiences);

            Assert.Contains(jwt.Claims, claim =>
                claim.Type == ClaimTypes.NameIdentifier &&
                claim.Value == userId.ToString());

            Assert.Contains(jwt.Claims, claim =>
                claim.Type == ClaimTypes.Email &&
                claim.Value == "test.user@example.com");

            Assert.Contains(jwt.Claims, claim =>
                claim.Type == ClaimTypes.Name &&
                claim.Value == "Test User");

            Assert.Contains(jwt.Claims, claim =>
                claim.Type == ClaimTypes.Role &&
                claim.Value == ApplicationRoles.Participant);
        }
    }
}