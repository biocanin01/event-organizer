using EventOrganizer.Api.Contracts.Registrations;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Registrations;
using EventOrganizer.Domain.Users;
using EventOrganizer.Infrastructure.Identity;
using EventOrganizer.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventOrganizer.Tests.Api
{
    public sealed class RegistrationEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public RegistrationEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Create_ForFuturePublishedEvent_ReturnsPendingRegistration()
        {
            var (eventId, _, participantUserId) = await SeedEventAsync();
            var client = CreateAuthenticatedClient(participantUserId, ApplicationRoles.Participant);

            var response = await client.PostAsync($"/api/events/{eventId}/registrations", null);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pending", payload.RootElement.GetProperty("status").GetString());
            Assert.Equal(1, payload.RootElement.GetProperty("version").GetInt32());
            Assert.Equal("Published", payload.RootElement.GetProperty("eventStatus").GetString());
            Assert.NotEqual(
                default,
                payload.RootElement.GetProperty("eventStartsAtUtc").GetDateTime());
        }

        [Fact]
        public async Task Create_WhenRegistrationAlreadyExists_ReturnsConflict()
        {
            var (eventId, _, participantUserId) = await SeedEventAsync();
            var client = CreateAuthenticatedClient(participantUserId, ApplicationRoles.Participant);
            await client.PostAsync($"/api/events/{eventId}/registrations", null);

            var response = await client.PostAsync($"/api/events/{eventId}/registrations", null);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenParticipantOwnsEvent_ReturnsConflict()
        {
            var (eventId, organizerUserId, _) = await SeedEventAsync();
            var client = CreateAuthenticatedClient(organizerUserId, ApplicationRoles.Participant);

            var response = await client.PostAsync($"/api/events/{eventId}/registrations", null);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenEventIsNotPublished_ReturnsConflict()
        {
            var (eventId, _, participantUserId) = await SeedEventAsync(publish: false);
            var client = CreateAuthenticatedClient(participantUserId, ApplicationRoles.Participant);

            var response = await client.PostAsync($"/api/events/{eventId}/registrations", null);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Create_AfterOwnCancellation_StillReturnsConflict()
        {
            var (eventId, _, participantUserId) = await SeedEventAsync();
            var client = CreateAuthenticatedClient(participantUserId, ApplicationRoles.Participant);
            var created = await client.PostAsync($"/api/events/{eventId}/registrations", null);
            using var payload = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var registrationId = payload.RootElement.GetProperty("id").GetGuid();
            var version = payload.RootElement.GetProperty("version").GetInt32();
            await client.PatchAsJsonAsync(
                $"/api/registrations/{registrationId}/cancel",
                new RegistrationVersionRequest(version));

            var response = await client.PostAsync($"/api/events/{eventId}/registrations", null);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Confirm_WhenCapacityIsReached_ReturnsConflict()
        {
            var secondParticipantUserId = Guid.NewGuid();
            var (eventId, organizerUserId, firstParticipantUserId) = await SeedEventAsync(
                capacity: 1,
                additionalUserIds: [secondParticipantUserId]);
            var firstRegistrationId = await CreateRegistrationAsync(eventId, firstParticipantUserId);
            var secondRegistrationId = await CreateRegistrationAsync(eventId, secondParticipantUserId);
            var organizerClient = CreateAuthenticatedClient(organizerUserId, ApplicationRoles.Organizer);

            var firstResponse = await organizerClient.PatchAsJsonAsync(
                $"/api/registrations/{firstRegistrationId}/confirm",
                new RegistrationVersionRequest(1));
            var secondResponse = await organizerClient.PatchAsJsonAsync(
                $"/api/registrations/{secondRegistrationId}/confirm",
                new RegistrationVersionRequest(1));

            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        }

        [Fact]
        public async Task Reject_ByEventOwner_StoresReason()
        {
            var (eventId, organizerUserId, participantUserId) = await SeedEventAsync();
            var registrationId = await CreateRegistrationAsync(eventId, participantUserId);
            var client = CreateAuthenticatedClient(organizerUserId, ApplicationRoles.Organizer);

            var response = await client.PatchAsJsonAsync(
                $"/api/registrations/{registrationId}/reject",
                new RejectRegistrationRequest("Kapacitet je rezervisan.", 1));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Rejected", payload.RootElement.GetProperty("status").GetString());
            Assert.Equal("Kapacitet je rezervisan.", payload.RootElement.GetProperty("rejectionReason").GetString());
        }

        [Fact]
        public async Task Cancel_WithStaleVersion_ReturnsConflict()
        {
            var (eventId, _, participantUserId) = await SeedEventAsync();
            var registrationId = await CreateRegistrationAsync(eventId, participantUserId);
            var client = CreateAuthenticatedClient(participantUserId, ApplicationRoles.Participant);

            var response = await client.PatchAsJsonAsync(
                $"/api/registrations/{registrationId}/cancel",
                new RegistrationVersionRequest(999));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task ListForEvent_WithOtherOrganizer_ReturnsForbidden()
        {
            var (eventId, _, _) = await SeedEventAsync();
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Organizer);

            var response = await client.GetAsync($"/api/events/{eventId}/registrations");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private async Task<(Guid EventId, Guid OrganizerUserId, Guid ParticipantUserId)> SeedEventAsync(
            int capacity = 20,
            IReadOnlyCollection<Guid>? additionalUserIds = null,
            bool publish = true)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var organizerUserId = Guid.NewGuid();
            var participantUserId = Guid.NewGuid();
            var startsAtUtc = DateTime.UtcNow.AddDays(10);
            var eventItem = Event.Create(
                $"Registration event {Guid.NewGuid():N}",
                "Event used by registration endpoint tests.",
                startsAtUtc,
                startsAtUtc.AddHours(3),
                capacity,
                1000m,
                "IT",
                1,
                organizerUserId,
                DateTime.UtcNow);
            if (publish)
            {
                eventItem.Publish(DateTime.UtcNow);
            }

            dbContext.Users.Add(CreateUser(organizerUserId, "Organizer"));
            dbContext.Users.Add(CreateUser(participantUserId, "Participant"));
            foreach (var userId in additionalUserIds ?? [])
            {
                dbContext.Users.Add(CreateUser(userId, "Participant"));
            }

            dbContext.Events.Add(eventItem);
            await dbContext.SaveChangesAsync();
            return (eventItem.Id, organizerUserId, participantUserId);
        }

        private async Task<Guid> CreateRegistrationAsync(Guid eventId, Guid participantUserId)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var registration = Registration.Create(eventId, participantUserId, DateTime.UtcNow);
            dbContext.Registrations.Add(registration);
            await dbContext.SaveChangesAsync();
            return registration.Id;
        }

        private static ApplicationUser CreateUser(Guid userId, string name)
        {
            return new ApplicationUser
            {
                Id = userId,
                UserName = $"{userId:N}@example.com",
                Email = $"{userId:N}@example.com",
                FullName = name,
                Status = UserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
            };
        }

        private HttpClient CreateAuthenticatedClient(Guid userId, string role)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
            return client;
        }
    }
}
