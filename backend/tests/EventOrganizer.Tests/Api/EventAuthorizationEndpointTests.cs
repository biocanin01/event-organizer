using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Api.Contracts.Events;
using EventOrganizer.Domain.Users;
using EventOrganizer.Infrastructure.Identity;
using EventOrganizer.Infrastructure.Persistance;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace EventOrganizer.Tests.Api
{
    public sealed class EventAuthorizationEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EventAuthorizationEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateEvent_WithoutAuthentication_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync(
                "/api/events",
                CreateValidRequest());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WithParticipantRole_ReturnsForbidden()
        {
            var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Participant);

            var response = await client.PostAsJsonAsync(
                "/api/events",
                CreateValidRequest());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WithOrganizerRole_ReturnsCreated()
        {
            var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Organizer);

            var response = await client.PostAsJsonAsync(
                "/api/events",
                CreateValidRequest());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WithAdminRole_ReturnsCreated()
        {
            var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Admin);

            var response = await client.PostAsJsonAsync(
                "/api/events",
                CreateValidRequest());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task ListPublishedEvents_WithoutAuthentication_ReturnsOk()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/events");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

        private static CreateEventRequest CreateValidRequest()
        {
            return new CreateEventRequest(
                "Clean Architecture Seminar",
                "Professional event about backend architecture.",
                new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 13, 0, 0, DateTimeKind.Utc),
                80);
        }
    }
}
