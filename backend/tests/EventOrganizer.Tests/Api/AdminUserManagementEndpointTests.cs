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
    public sealed class AdminUserManagementEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AdminUserManagementEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ListUsers_WithoutAuthentication_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/admin/users");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ListUsers_WithParticipantRole_ReturnsForbidden()
        {
            var participantUserId = await CreateUserWithRolesAsync(ApplicationRoles.Participant);
            var client = CreateAuthenticatedClient(participantUserId, ApplicationRoles.Participant);

            var response = await client.GetAsync("/api/admin/users");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ListUsers_WithAdminRole_ReturnsUsers()
        {
            var adminUserId = await CreateUserWithRolesAsync(ApplicationRoles.Admin);
            var participantUserId = await CreateUserWithRolesAsync(ApplicationRoles.Participant);
            var client = CreateAuthenticatedClient(adminUserId, ApplicationRoles.Admin);

            var response = await client.GetAsync("/api/admin/users");
            var users = await response.Content.ReadFromJsonAsync<List<UserResponse>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(users);
            Assert.Contains(users, user => user.Id == participantUserId);
        }

        [Fact]
        public async Task ListUsers_WithCombinedFilters_ReturnsOnlyMatchingUsers()
        {
            var adminUserId = await CreateUserWithRolesAsync(ApplicationRoles.Admin);
            var searchMarker = Guid.NewGuid().ToString("N");
            var matchingUserId = await CreateUserAsync(
                $"Suspended Participant {searchMarker}",
                UserStatus.Suspended,
                ApplicationRoles.Participant);
            await CreateUserAsync(
                $"Active Organizer {searchMarker}",
                UserStatus.Active,
                ApplicationRoles.Participant,
                ApplicationRoles.Organizer);
            var client = CreateAuthenticatedClient(adminUserId, ApplicationRoles.Admin);

            var response = await client.GetAsync(
                $"/api/admin/users?search={searchMarker}&status=Suspended&role={ApplicationRoles.Participant}");
            var users = await response.Content.ReadFromJsonAsync<List<UserResponse>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var user = Assert.Single(users!);
            Assert.Equal(matchingUserId, user.Id);
            Assert.Equal(UserStatus.Suspended.ToString(), user.Status);
        }

        [Fact]
        public async Task Suspend_WithAdminRole_SuspendsUser()
        {
            var adminUserId = await CreateUserWithRolesAsync(ApplicationRoles.Admin);
            var participantUserId = await CreateUserWithRolesAsync(ApplicationRoles.Participant);
            var client = CreateAuthenticatedClient(adminUserId, ApplicationRoles.Admin);

            var response = await client.PatchAsync(
                $"/api/admin/users/{participantUserId}/suspend",
                null);
            var status = await GetUserStatusAsync(participantUserId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(UserStatus.Suspended, status);
        }

        [Fact]
        public async Task Suspend_WhenTargetIsCurrentAdmin_ReturnsConflict()
        {
            var adminUserId = await CreateUserWithRolesAsync(ApplicationRoles.Admin);
            var client = CreateAuthenticatedClient(adminUserId, ApplicationRoles.Admin);

            var response = await client.PatchAsync(
                $"/api/admin/users/{adminUserId}/suspend",
                null);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        private HttpClient CreateAuthenticatedClient(Guid userId, string role)
        {
            var client = _factory.CreateClient();

            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);

            return client;
        }

        private async Task<Guid> CreateUserWithRolesAsync(params string[] roles)
        {
            return await CreateUserAsync("Test User", UserStatus.Active, roles);
        }

        private async Task<Guid> CreateUserAsync(
            string fullName,
            UserStatus status,
            params string[] roles)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var email = $"{Guid.NewGuid():N}@example.com";
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FullName = fullName,
                Status = status,
                CreatedAtUtc = DateTime.UtcNow,
                VerifiedAtUtc = DateTime.UtcNow,
            };

            var createResult = await userManager.CreateAsync(user);
            Assert.True(createResult.Succeeded);

            foreach (var role in roles)
            {
                var roleResult = await userManager.AddToRoleAsync(user, role);
                Assert.True(roleResult.Succeeded);
            }

            return user.Id;
        }

        private async Task<UserStatus> GetUserStatusAsync(Guid userId)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId.ToString());

            Assert.NotNull(user);

            return user.Status;
        }
    }
}
