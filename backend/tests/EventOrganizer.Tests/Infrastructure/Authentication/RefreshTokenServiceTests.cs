using EventOrganizer.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace EventOrganizer.Tests.Infrastructure.Authentication
{
    public sealed class RefreshTokenServiceTests
    {
        [Fact]
        public void CreateRefreshToken_ReturnsTokenAndHash()
        {
            var service = CreateService();

            var result = service.CreateRefreshToken();

            Assert.False(string.IsNullOrWhiteSpace(result.Token));
            Assert.False(string.IsNullOrWhiteSpace(result.TokenHash));
            Assert.NotEqual(result.Token, result.TokenHash);
        }

        [Fact]
        public void CreateRefreshToken_WhenCalledMultipleTimes_ReturnsUniqueTokens()
        {
            var service = CreateService();

            var firstToken = service.CreateRefreshToken();
            var secondToken = service.CreateRefreshToken();

            Assert.NotEqual(firstToken.Token, secondToken.Token);
            Assert.NotEqual(firstToken.TokenHash, secondToken.TokenHash);
        }

        [Fact]
        public void HashToken_WithSameToken_ReturnsSameHash()
        {
            var service = CreateService();

            var firstHash = service.HashToken("refresh-token");
            var secondHash = service.HashToken("refresh-token");

            Assert.Equal(firstHash, secondHash);
        }

        [Fact]
        public void HashToken_WithDifferentTokens_ReturnsDifferentHashes()
        {
            var service = CreateService();

            var firstHash = service.HashToken("first-refresh-token");
            var secondHash = service.HashToken("second-refresh-token");

            Assert.NotEqual(firstHash, secondHash);
        }

        [Fact]
        public void CreateRefreshToken_UsesConfiguredExpirationDays()
        {
            var service = CreateService(refreshTokenExpirationDays: 7);

            var beforeCreation = DateTime.UtcNow.AddDays(7).AddSeconds(-1);
            var result = service.CreateRefreshToken();
            var afterCreation = DateTime.UtcNow.AddDays(7).AddSeconds(1);

            Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
            Assert.InRange(result.ExpiresAtUtc, beforeCreation, afterCreation);
        }

        private static RefreshTokenService CreateService(int refreshTokenExpirationDays = 7)
        {
            var settings = new JwtSettings
            {
                Issuer = "EventOrganizer",
                Audience = "EventOrganizerClient",
                SigningKey = "test-signing-key-for-refresh-token-service-tests-12345",
                AccessTokenExpirationMinutes = 15,
                RefreshTokenExpirationDays = refreshTokenExpirationDays,
            };

            return new RefreshTokenService(Options.Create(settings));
        }
    }
}
