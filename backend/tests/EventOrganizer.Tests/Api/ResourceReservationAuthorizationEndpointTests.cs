using EventOrganizer.Api.Contracts.ResourceReservations;
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
    public sealed class ResourceReservationAuthorizationEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ResourceReservationAuthorizationEndpointTests(
            CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateReservation_WithoutAuthentication_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync(
                "/api/resource-reservations",
                await CreateValidRequestAsync());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateReservation_WithParticipantRole_ReturnsForbidden()
        {
            var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Participant);

            var response = await client.PostAsJsonAsync(
                "/api/resource-reservations",
                await CreateValidRequestAsync());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateReservation_WithOrganizerRole_ReturnsCreated()
        {
            var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Organizer);

            var response = await client.PostAsJsonAsync(
                "/api/resource-reservations",
                await CreateValidRequestAsync());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateReservation_WithAdminRole_ReturnsCreated()
        {
            var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Admin);

            var response = await client.PostAsJsonAsync(
                "/api/resource-reservations",
                await CreateValidRequestAsync());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task ConfirmReservation_WithParticipantRole_ReturnsForbidden()
        {
            var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Participant);

            var response = await client.PatchAsync(
                $"/api/resource-reservations/{Guid.NewGuid()}/confirm",
                null);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ConfirmReservation_WithAdminRole_ReturnsNoContent()
        {
            var reservationId = await CreateReservationAsync();
            var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Admin);

            var response = await client.PatchAsync(
                $"/api/resource-reservations/{reservationId}/confirm",
                null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task CancelReservation_WithoutAuthentication_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();

            var response = await client.PatchAsync(
                $"/api/resource-reservations/{Guid.NewGuid()}/cancel",
                null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CancelReservation_WithParticipantRole_ReturnsForbidden()
        {
            var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Participant);

            var response = await client.PatchAsync(
                $"/api/resource-reservations/{Guid.NewGuid()}/cancel",
                null);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CancelReservation_WithOrganizerRoleAndOwnReservation_ReturnsNoContent()
        {
            var organizerUserId = Guid.NewGuid();
            var client = await CreateAuthenticatedClientAsync(
                organizerUserId,
                ApplicationRoles.Organizer);
            var reservationId = await CreateReservationAsync(organizerUserId);

            var response = await client.PatchAsync(
                $"/api/resource-reservations/{reservationId}/cancel",
                null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task CancelReservation_WithAdminRole_ReturnsNoContent()
        {
            var reservationId = await CreateReservationAsync();
            var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Admin);

            var response = await client.PatchAsync(
                $"/api/resource-reservations/{reservationId}/cancel",
                null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
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

        private async Task<CreateResourceReservationRequest> CreateValidRequestAsync()
        {
            var data = await CreateEventAndResourceAsync();
            var startsAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);

            return new CreateResourceReservationRequest(
                data.EventId,
                data.ResourceId,
                startsAtUtc,
                startsAtUtc.AddHours(2));
        }

        private async Task<Guid> CreateReservationAsync(Guid? organizerUserId = null)
        {
            var data = await CreateEventAndResourceAsync(organizerUserId);
            var startsAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);

            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var reservation = ResourceReservation.Create(
                data.EventId,
                data.ResourceId,
                startsAtUtc,
                startsAtUtc.AddHours(2),
                DateTime.UtcNow);

            dbContext.ResourceReservations.Add(reservation);
            await dbContext.SaveChangesAsync();

            return reservation.Id;
        }

        private async Task<(Guid EventId, Guid ResourceId)> CreateEventAndResourceAsync(
            Guid? organizerUserId = null)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var resolvedOrganizerUserId = organizerUserId ?? Guid.NewGuid();

            if (organizerUserId is null)
            {
                dbContext.Users.Add(new ApplicationUser
                {
                    Id = resolvedOrganizerUserId,
                    UserName = $"{Guid.NewGuid():N}@example.com",
                    Email = $"{Guid.NewGuid():N}@example.com",
                    FullName = "Test Organizer",
                    Status = UserStatus.Active,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }

            var eventItem = EventOrganizer.Domain.Events.Event.Create(
                "Clean Architecture Seminar",
                "Professional event about backend architecture.",
                new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 13, 0, 0, DateTimeKind.Utc),
                80,
                resolvedOrganizerUserId,
                DateTime.UtcNow);

            var resource = Resource.Create(
                $"Conference Hall {Guid.NewGuid():N}",
                "A hall suitable for conferences.",
                ResourceType.Venue,
                DateTime.UtcNow);

            dbContext.Events.Add(eventItem);
            dbContext.Resources.Add(resource);
            await dbContext.SaveChangesAsync();

            return (eventItem.Id, resource.Id);
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
    }
}
