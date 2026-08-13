using EventOrganizer.Api.Contracts.Events;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
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
    public sealed class EventManagementEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EventManagementEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ListManageable_WithOrganizer_ReturnsOwnEvents()
        {
            var organizerUserId = Guid.NewGuid();
            var ownEventId = await SeedEventAsync(organizerUserId);
            await SeedEventAsync(Guid.NewGuid(), "Other Event");
            var client = CreateAuthenticatedClient(organizerUserId, ApplicationRoles.Organizer);

            var response = await client.GetAsync("/api/events/manage");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var eventItem = Assert.Single(payload.RootElement.EnumerateArray());
            Assert.Equal(ownEventId, eventItem.GetProperty("id").GetGuid());
        }

        [Fact]
        public async Task ListManageable_WithAdmin_ReturnsEvents()
        {
            await SeedEventAsync(Guid.NewGuid());
            await SeedEventAsync(Guid.NewGuid(), "Second Event");
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Admin);

            var response = await client.GetAsync("/api/events/manage");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetArrayLength() >= 2);
        }

        [Fact]
        public async Task Update_WithDraftBooking_ReturnsNoContent()
        {
            var organizerUserId = Guid.NewGuid();
            var eventId = await SeedEventAsync(organizerUserId, includeDraftBooking: true);
            var client = CreateAuthenticatedClient(organizerUserId, ApplicationRoles.Organizer);
            var startsAtUtc = DateTime.UtcNow.AddDays(20);

            var response = await client.PutAsJsonAsync(
                $"/api/events/{eventId}",
                new UpdateEventRequest(
                    "Updated Event",
                    "Updated description.",
                    startsAtUtc,
                    startsAtUtc.AddHours(3),
                    120,
                    1500m,
                    "Finance",
                    2,
                    true));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Publish_WithoutApprovedBooking_ReturnsConflict()
        {
            var organizerUserId = Guid.NewGuid();
            var eventId = await SeedEventAsync(organizerUserId, includeDraftBooking: true);
            var client = CreateAuthenticatedClient(organizerUserId, ApplicationRoles.Organizer);

            var response = await client.PatchAsync($"/api/events/{eventId}/publish", null);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Complete_WhenPublishedEventHasEnded_ReturnsNoContent()
        {
            var organizerUserId = Guid.NewGuid();
            var eventId = await SeedEventAsync(
                organizerUserId,
                startsAtUtc: DateTime.UtcNow.AddHours(-4),
                endsAtUtc: DateTime.UtcNow.AddHours(-2),
                status: EventStatus.Published,
                includeDraftBooking: true,
                bookingStatus: EventResourceBookingStatus.Approved);
            var client = CreateAuthenticatedClient(organizerUserId, ApplicationRoles.Organizer);

            var response = await client.PatchAsync($"/api/events/{eventId}/complete", null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Complete_WhenEventHasNotEnded_ReturnsConflict()
        {
            var organizerUserId = Guid.NewGuid();
            var eventId = await SeedEventAsync(
                organizerUserId,
                startsAtUtc: DateTime.UtcNow.AddHours(-1),
                endsAtUtc: DateTime.UtcNow.AddHours(1),
                status: EventStatus.Published);
            var client = CreateAuthenticatedClient(organizerUserId, ApplicationRoles.Organizer);

            var response = await client.PatchAsync($"/api/events/{eventId}/complete", null);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Complete_WithParticipant_ReturnsForbidden()
        {
            var organizerUserId = Guid.NewGuid();
            var eventId = await SeedEventAsync(
                organizerUserId,
                startsAtUtc: DateTime.UtcNow.AddHours(-4),
                endsAtUtc: DateTime.UtcNow.AddHours(-2),
                status: EventStatus.Published);
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Participant);

            var response = await client.PatchAsync($"/api/events/{eventId}/complete", null);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private async Task<Guid> SeedEventAsync(
            Guid organizerUserId,
            string title = "Managed Event",
            DateTime? startsAtUtc = null,
            DateTime? endsAtUtc = null,
            EventStatus status = EventStatus.Draft,
            bool includeDraftBooking = false,
            EventResourceBookingStatus? bookingStatus = null)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var resolvedStartsAtUtc = startsAtUtc ?? DateTime.UtcNow.AddDays(10);
            var eventItem = Event.Create(
                $"{title} {Guid.NewGuid():N}",
                "Event used by management endpoint tests.",
                resolvedStartsAtUtc,
                endsAtUtc ?? resolvedStartsAtUtc.AddHours(4),
                80,
                1000m,
                "IT",
                1,
                organizerUserId,
                DateTime.UtcNow);
            var organizer = new ApplicationUser
            {
                Id = organizerUserId,
                UserName = $"{organizerUserId:N}@example.com",
                Email = $"{organizerUserId:N}@example.com",
                FullName = "Event Organizer",
                Status = UserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
            };

            dbContext.Users.Add(organizer);
            dbContext.Events.Add(eventItem);

            EventResourceBooking? booking = null;
            if (includeDraftBooking)
            {
                booking = EventResourceBooking.Create(eventItem.Id, DateTime.UtcNow);
                dbContext.EventResourceBookings.Add(booking);
            }

            await dbContext.SaveChangesAsync();

            if (booking is not null && bookingStatus.HasValue)
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE EventResourceBookings SET Status = {bookingStatus.Value.ToString()} WHERE Id = {booking.Id}");
            }

            if (status == EventStatus.Published)
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE Events SET Status = {status.ToString()} WHERE Id = {eventItem.Id}");
            }

            return eventItem.Id;
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
