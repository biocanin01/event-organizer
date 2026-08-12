using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Resources;
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
    public sealed class AdminBookingEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AdminBookingEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task List_WithAdmin_ReturnsBookings()
        {
            var scenario = await SeedSubmittedBookingAsync();
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Admin);

            var response = await client.GetAsync("/api/bookings?status=Submitted");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Contains(
                payload.RootElement.EnumerateArray(),
                item => item.GetProperty("id").GetGuid() == scenario.BookingId);
        }

        [Fact]
        public async Task GetById_WithAdmin_ReturnsDecisionFields()
        {
            var scenario = await SeedSubmittedBookingAsync();
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Admin);

            var response = await client.GetAsync($"/api/bookings/{scenario.BookingId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(scenario.BookingId, payload.RootElement.GetProperty("id").GetGuid());
            Assert.True(payload.RootElement.TryGetProperty("decisionReason", out _));
            Assert.True(payload.RootElement.TryGetProperty("decidedAtUtc", out _));
            Assert.True(payload.RootElement.TryGetProperty("decidedByUserId", out _));
        }

        [Fact]
        public async Task Approve_WithAdmin_ReturnsApprovedBooking()
        {
            var scenario = await SeedSubmittedBookingAsync();
            var adminUserId = Guid.NewGuid();
            var client = CreateAuthenticatedClient(adminUserId, ApplicationRoles.Admin);

            var response = await client.PatchAsJsonAsync(
                $"/api/bookings/{scenario.BookingId}/approve",
                new { scenario.Version });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Approved", payload.RootElement.GetProperty("status").GetString());
            Assert.Equal(adminUserId, payload.RootElement.GetProperty("decidedByUserId").GetGuid());
        }

        [Fact]
        public async Task Reject_WithAdmin_ReturnsRejectedBooking()
        {
            var scenario = await SeedSubmittedBookingAsync();
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Admin);

            var response = await client.PatchAsJsonAsync(
                $"/api/bookings/{scenario.BookingId}/reject",
                new
                {
                    scenario.Version,
                    Reason = "Budget needs review.",
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Rejected", payload.RootElement.GetProperty("status").GetString());
            Assert.Equal("Budget needs review.", payload.RootElement.GetProperty("decisionReason").GetString());
        }

        [Fact]
        public async Task Expire_WithAdmin_ReturnsExpiredCount()
        {
            await SeedSubmittedBookingAsync(holdExpiresAtUtc: DateTime.UtcNow.AddHours(-1));
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Admin);

            var response = await client.PatchAsync("/api/bookings/expire", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("expiredCount").GetInt32());
        }

        [Theory]
        [InlineData(ApplicationRoles.Organizer)]
        [InlineData(ApplicationRoles.Participant)]
        public async Task List_WithNonAdmin_ReturnsForbidden(string role)
        {
            await SeedSubmittedBookingAsync();
            var client = CreateAuthenticatedClient(Guid.NewGuid(), role);

            var response = await client.GetAsync("/api/bookings");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Approve_WithStaleVersion_ReturnsConflict()
        {
            var scenario = await SeedSubmittedBookingAsync();
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Admin);

            var response = await client.PatchAsJsonAsync(
                $"/api/bookings/{scenario.BookingId}/approve",
                new { Version = 999 });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        private async Task<BookingScenario> SeedSubmittedBookingAsync(
            DateTime? holdExpiresAtUtc = null)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var organizerUserId = Guid.NewGuid();
            var startsAtUtc = DateTime.UtcNow.AddDays(10);
            var organizer = new ApplicationUser
            {
                Id = organizerUserId,
                UserName = $"{organizerUserId:N}@example.com",
                Email = $"{organizerUserId:N}@example.com",
                FullName = "Booking Organizer",
                Status = UserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
            };
            var eventItem = Event.Create(
                $"Admin Booking Event {Guid.NewGuid():N}",
                "Event used to verify admin booking endpoints.",
                startsAtUtc,
                startsAtUtc.AddHours(4),
                80,
                1000m,
                "IT",
                1,
                organizerUserId,
                DateTime.UtcNow);
            var venue = TestResourceFactory.Create(
                $"Admin Booking Venue {Guid.NewGuid():N}",
                "Venue used by admin booking endpoint tests.",
                ResourceType.Venue,
                200m,
                120,
                null,
                4,
                DateTime.UtcNow);
            var speaker = TestResourceFactory.Create(
                $"Admin Booking Speaker {Guid.NewGuid():N}",
                "Speaker used by admin booking endpoint tests.",
                ResourceType.Speaker,
                100m,
                null,
                "IT",
                4,
                DateTime.UtcNow);
            var booking = EventResourceBooking.Create(eventItem.Id, DateTime.UtcNow);
            booking.AddResource(venue.Id, ResourceType.Venue, DateTime.UtcNow);
            booking.AddResource(speaker.Id, ResourceType.Speaker, DateTime.UtcNow);

            dbContext.Users.Add(organizer);
            dbContext.Events.Add(eventItem);
            dbContext.Resources.AddRange(venue, speaker);
            dbContext.EventResourceBookings.Add(booking);
            await dbContext.SaveChangesAsync();

            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE EventResourceBookings SET Status = {EventResourceBookingStatus.Submitted.ToString()}, HoldExpiresAtUtc = {holdExpiresAtUtc ?? DateTime.UtcNow.AddHours(1)} WHERE Id = {booking.Id}");

            dbContext.ChangeTracker.Clear();
            var reloadedBooking = await dbContext.EventResourceBookings
                .AsNoTracking()
                .SingleAsync(storedBooking => storedBooking.Id == booking.Id);

            return new BookingScenario(
                reloadedBooking.Id,
                reloadedBooking.Version);
        }

        private HttpClient CreateAuthenticatedClient(Guid userId, string role)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
            return client;
        }

        private sealed record BookingScenario(Guid BookingId, int Version);
    }
}
