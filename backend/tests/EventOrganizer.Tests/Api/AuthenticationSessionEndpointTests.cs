using EventOrganizer.Api.Contracts.Auth;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Users;
using EventOrganizer.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace EventOrganizer.Tests.Api
{
    public sealed class AuthenticationSessionEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuthenticationSessionEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Logout_RevokesRefreshTokenAndPreventsReuse()
        {
            var client = _factory.CreateClient();
            var email = $"{Guid.NewGuid():N}@example.com";
            var registerResponse = await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest("Session User", email, "Password1"));
            var authentication = await registerResponse.Content
                .ReadFromJsonAsync<AuthResponse>();

            Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
            Assert.NotNull(authentication?.RefreshToken);

            var logoutResponse = await client.PostAsJsonAsync(
                "/api/auth/logout",
                new LogoutRequest(authentication.RefreshToken));
            var refreshResponse = await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(authentication.RefreshToken));

            Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
        }

        [Fact]
        public async Task ReactivatedUser_CannotReuseSessionIssuedBeforeSuspension()
        {
            var anonymousClient = _factory.CreateClient();
            var email = $"{Guid.NewGuid():N}@example.com";
            const string password = "Password1";
            var registerResponse = await anonymousClient.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest("Suspended User", email, password));
            var authentication = await registerResponse.Content
                .ReadFromJsonAsync<AuthResponse>();
            var adminUserId = await CreateAdminUserAsync();
            var adminClient = CreateAuthenticatedAdminClient(adminUserId);

            Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
            Assert.NotNull(authentication?.RefreshToken);

            var suspendResponse = await adminClient.PatchAsync(
                $"/api/admin/users/{authentication.UserId}/suspend",
                null);
            var reactivateResponse = await adminClient.PatchAsync(
                $"/api/admin/users/{authentication.UserId}/reactivate",
                null);
            var oldSessionRefreshResponse = await anonymousClient.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(authentication.RefreshToken));
            var newLoginResponse = await anonymousClient.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(email, password));

            Assert.Equal(HttpStatusCode.NoContent, suspendResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, reactivateResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, oldSessionRefreshResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, newLoginResponse.StatusCode);
        }

        private HttpClient CreateAuthenticatedAdminClient(Guid adminUserId)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(
                TestAuthHandler.UserIdHeader,
                adminUserId.ToString());
            client.DefaultRequestHeaders.Add(
                TestAuthHandler.RoleHeader,
                ApplicationRoles.Admin);

            return client;
        }

        private async Task<Guid> CreateAdminUserAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var email = $"{Guid.NewGuid():N}@example.com";
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FullName = "Session Admin",
                Status = UserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
                VerifiedAtUtc = DateTime.UtcNow,
            };

            var createResult = await userManager.CreateAsync(user);
            Assert.True(createResult.Succeeded);
            var roleResult = await userManager.AddToRoleAsync(
                user,
                ApplicationRoles.Admin);
            Assert.True(roleResult.Succeeded);

            return user.Id;
        }
    }
}
