using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace EventOrganizer.Tests.Api
{
    public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";
        public const string UserIdHeader = "X-Test-User-Id";
        public const string RoleHeader = "X-Test-User-Role";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(UserIdHeader, out var userIdValues) ||
                !Request.Headers.TryGetValue(RoleHeader, out var roleValues))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!Guid.TryParse(userIdValues.FirstOrDefault(), out var userId))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid test user id."));
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Email, "test-user@example.com"),
                new(ClaimTypes.Name, "Test User"),
            };

            claims.AddRange(roleValues
                .SelectMany(value => value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    ?? [])
                .Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
