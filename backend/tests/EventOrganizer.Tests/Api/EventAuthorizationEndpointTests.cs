using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Api.Contracts.Events;
using EventOrganizer.Domain.Resources;
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

        [Fact]
        public async Task GetRecommendation_WithoutAuthentication_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync(
                $"/api/events/{Guid.NewGuid()}/recommendation");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetRecommendation_WithParticipantRole_ReturnsForbidden()
        {
            var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Participant);

            var response = await client.GetAsync(
                $"/api/events/{Guid.NewGuid()}/recommendation");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetRecommendation_WithOrganizerRoleAndOwnEvent_ReturnsOk()
        {
            var organizerUserId = Guid.NewGuid();
            var client = await CreateAuthenticatedClientAsync(
                organizerUserId,
                ApplicationRoles.Organizer);
            var eventId = await CreateEventWithRecommendationResourcesAsync(organizerUserId);

            var response = await client.GetAsync(
                $"/api/events/{eventId}/recommendation");

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

        private async Task<HttpClient> CreateAuthenticatedClientAsync(
            Guid userId,
            string role)
        {
            var client = _factory.CreateClient();

            await CreateTestUserAsync(userId);

            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);

            return client;
        }

        private async Task<Guid> CreateEventWithRecommendationResourcesAsync(Guid organizerUserId)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var startsAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);

            var eventItem = EventOrganizer.Domain.Events.Event.Create(
                "Clean Architecture Seminar",
                "Professional event about backend architecture.",
                startsAtUtc,
                startsAtUtc.AddHours(4),
                80,
                1000m,
                "IT",
                1,
                organizerUserId,
                DateTime.UtcNow);

            var venue = Resource.Create(
                $"Conference Hall {Guid.NewGuid():N}",
                "A hall suitable for conferences.",
                ResourceType.Venue,
                300m,
                120,
                "IT",
                5,
                DateTime.UtcNow);

            var speaker = Resource.Create(
                $"Architecture Speaker {Guid.NewGuid():N}",
                "A speaker for architecture events.",
                ResourceType.Speaker,
                200m,
                null,
                "IT",
                5,
                DateTime.UtcNow);

            dbContext.Events.Add(eventItem);
            dbContext.Resources.AddRange(venue, speaker);
            await dbContext.SaveChangesAsync();

            return eventItem.Id;
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
                80,
                1000m,
                "IT",
                1);
        }
    }
}
