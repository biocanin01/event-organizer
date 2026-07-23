using EventOrganizer.Api.Contracts.Resources;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Domain.Resources;
using EventOrganizer.Domain.Users;
using EventOrganizer.Infrastructure.Identity;
using EventOrganizer.Infrastructure.Persistance;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace EventOrganizer.Tests.Api
{
    public sealed class ResourceAuthorizationEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ResourceAuthorizationEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateResource_WithoutAuthentication_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync(
                "/api/resources",
                CreateValidRequest());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateResource_WithParticipantRole_ReturnsForbidden()
        {
            var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Participant);

            var response = await client.PostAsJsonAsync(
                "/api/resources",
                CreateValidRequest());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateResource_WithOrganizerRole_ReturnsForbidden()
        {
            var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Organizer);

            var response = await client.PostAsJsonAsync(
                "/api/resources",
                CreateValidRequest());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateResource_WithAdminRole_ReturnsCreated()
        {
            var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Admin);

            var response = await client.PostAsJsonAsync(
                "/api/resources",
                CreateValidRequest());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task ArchiveResource_WithParticipantRole_ReturnsForbidden()
        {
            var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Participant);

            var response = await client.PatchAsync(
                $"/api/resources/{Guid.NewGuid()}/archive",
                null);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private async Task<HttpClient> CreateAuthenticatedClientAsync(string role)
        {
            var client = _factory.CreateClient();
            var userId = Guid.NewGuid();

            await CreateTestUserAsync(userId);

            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);

            return client;
        }

        private async Task CreateTestUserAsync(Guid userId)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Users.Add(new ApplicationUser
            {
                Id = userId,
                UserName = $"{userId:N}@example.com",
                Email = $"{userId:N}@example.com",
                FullName = "Test User",
                Status = UserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await dbContext.SaveChangesAsync();
        }

        private static CreateResourceRequest CreateValidRequest()
        {
            return new CreateResourceRequest(
                "Main Conference Hall",
                "A hall suitable for conferences with up to 200 participants.",
                ResourceType.Venue);
        }
    }
}
