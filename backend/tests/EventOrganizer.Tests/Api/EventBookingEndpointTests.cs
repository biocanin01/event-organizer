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
    public sealed class EventBookingEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EventBookingEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetBooking_WithOwnerOrganizer_ReturnsOk()
        {
            var scenario = await SeedScenarioAsync();
            var client = CreateAuthenticatedClient(scenario.OrganizerUserId, ApplicationRoles.Organizer);

            var response = await client.GetAsync($"/api/events/{scenario.EventId}/booking");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetBooking_WithAdmin_ReturnsOk()
        {
            var scenario = await SeedScenarioAsync();
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Admin);

            var response = await client.GetAsync($"/api/events/{scenario.EventId}/booking");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateDraft_WithAdmin_ReturnsForbidden()
        {
            var scenario = await SeedScenarioAsync();
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Admin);

            var response = await client.PutAsJsonAsync(
                $"/api/events/{scenario.EventId}/booking/draft",
                new
                {
                    scenario.Version,
                    VenueId = (Guid?)null,
                    SpeakerIds = Array.Empty<Guid>(),
                    EquipmentPackageId = (Guid?)null,
                });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateDraft_WithNullSpeakerIds_ReturnsBadRequest()
        {
            var scenario = await SeedScenarioAsync();
            var client = CreateAuthenticatedClient(scenario.OrganizerUserId, ApplicationRoles.Organizer);

            var response = await client.PutAsJsonAsync(
                $"/api/events/{scenario.EventId}/booking/draft",
                new
                {
                    scenario.Version,
                    VenueId = (Guid?)null,
                    SpeakerIds = (Guid[]?)null,
                    EquipmentPackageId = (Guid?)null,
                });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Submit_WithOverlappingApprovedBooking_ReturnsStructuredConflict()
        {
            var scenario = await SeedScenarioAsync(includeApprovedConflict: true);
            var client = CreateAuthenticatedClient(scenario.OrganizerUserId, ApplicationRoles.Organizer);

            var response = await client.PatchAsJsonAsync(
                $"/api/events/{scenario.EventId}/booking/submit",
                new { scenario.Version });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var conflict = Assert.Single(payload.RootElement.GetProperty("conflicts").EnumerateArray());
            Assert.Equal(scenario.VenueId, conflict.GetProperty("resourceId").GetGuid());
            Assert.Equal(scenario.ConflictingEventId, conflict.GetProperty("eventId").GetGuid());
        }

        private async Task<BookingScenario> SeedScenarioAsync(bool includeApprovedConflict = false)
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
                $"Booking Event {Guid.NewGuid():N}",
                "Event used to verify booking endpoints.",
                startsAtUtc,
                startsAtUtc.AddHours(4),
                80,
                1000m,
                "IT",
                1,
                organizerUserId,
                DateTime.UtcNow);
            var venue = TestResourceFactory.Create(
                $"Booking Venue {Guid.NewGuid():N}",
                "Venue used by booking endpoint tests.",
                ResourceType.Venue,
                200m,
                120,
                null,
                4,
                DateTime.UtcNow);
            var speaker = TestResourceFactory.Create(
                $"Booking Speaker {Guid.NewGuid():N}",
                "Speaker used by booking endpoint tests.",
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

            Guid? conflictingEventId = null;
            EventResourceBooking? conflictingBooking = null;
            if (includeApprovedConflict)
            {
                var conflictingEvent = Event.Create(
                    $"Conflicting Event {Guid.NewGuid():N}",
                    "Overlapping event used by booking endpoint tests.",
                    startsAtUtc.AddHours(1),
                    startsAtUtc.AddHours(3),
                    80,
                    1000m,
                    "IT",
                    1,
                    organizerUserId,
                    DateTime.UtcNow);
                conflictingBooking = EventResourceBooking.Create(
                    conflictingEvent.Id,
                    DateTime.UtcNow);
                conflictingBooking.AddResource(
                    venue.Id,
                    ResourceType.Venue,
                    DateTime.UtcNow);
                conflictingEventId = conflictingEvent.Id;
                dbContext.Events.Add(conflictingEvent);
                dbContext.EventResourceBookings.Add(conflictingBooking);
            }

            await dbContext.SaveChangesAsync();

            if (conflictingBooking is not null)
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE EventResourceBookings SET Status = {EventResourceBookingStatus.Approved.ToString()} WHERE Id = {conflictingBooking.Id}");
            }

            return new BookingScenario(
                organizerUserId,
                eventItem.Id,
                booking.Version,
                venue.Id,
                conflictingEventId);
        }

        private HttpClient CreateAuthenticatedClient(Guid userId, string role)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
            return client;
        }

        private sealed record BookingScenario(
            Guid OrganizerUserId,
            Guid EventId,
            int Version,
            Guid VenueId,
            Guid? ConflictingEventId);
    }
}
