using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Registrations;
using EventOrganizer.Domain.Reviews;
using EventOrganizer.Domain.Users;
using EventOrganizer.Infrastructure.Identity;
using EventOrganizer.Infrastructure.Persistance;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EventOrganizer.Tests.Api
{
    public sealed class EventInsightEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EventInsightEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task List_ForOrganizer_ReturnsOnlyOwnedEvents()
        {
            var ownerUserId = Guid.NewGuid();
            var otherOrganizerUserId = Guid.NewGuid();
            var ownedEventId = await SeedEventAsync(ownerUserId, "Owned insight event");
            var otherEventId = await SeedEventAsync(otherOrganizerUserId, "Other insight event");
            var client = CreateAuthenticatedClient(ownerUserId, ApplicationRoles.Organizer);

            var response = await client.GetAsync("/api/insights/events");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Contains(
                payload.RootElement.EnumerateArray(),
                insight => insight.GetProperty("eventId").GetGuid() == ownedEventId);
            Assert.DoesNotContain(
                payload.RootElement.EnumerateArray(),
                insight => insight.GetProperty("eventId").GetGuid() == otherEventId);
        }

        [Fact]
        public async Task List_ForAdmin_ReturnsAllEvents()
        {
            var firstEventId = await SeedEventAsync(Guid.NewGuid(), "First admin insight event");
            var secondEventId = await SeedEventAsync(Guid.NewGuid(), "Second admin insight event");
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Admin);

            var response = await client.GetAsync("/api/insights/events");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var eventIds = payload.RootElement
                .EnumerateArray()
                .Select(insight => insight.GetProperty("eventId").GetGuid())
                .ToArray();
            Assert.Contains(firstEventId, eventIds);
            Assert.Contains(secondEventId, eventIds);
        }

        [Fact]
        public async Task List_ForParticipant_ReturnsForbidden()
        {
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Participant);

            var response = await client.GetAsync("/api/insights/events");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetById_ReturnsRegistrationCountsRatingsAndRecentReviews()
        {
            var organizerUserId = Guid.NewGuid();
            var eventId = await SeedEventWithInsightDataAsync(organizerUserId);
            var client = CreateAuthenticatedClient(organizerUserId, ApplicationRoles.Organizer);

            var response = await client.GetAsync($"/api/insights/events/{eventId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = payload.RootElement;
            Assert.Equal(eventId, root.GetProperty("eventId").GetGuid());
            Assert.Equal(1, root.GetProperty("pendingRegistrationCount").GetInt32());
            Assert.Equal(2, root.GetProperty("confirmedRegistrationCount").GetInt32());
            Assert.Equal(1, root.GetProperty("rejectedRegistrationCount").GetInt32());
            Assert.Equal(1, root.GetProperty("cancelledRegistrationCount").GetInt32());
            Assert.Equal(40m, root.GetProperty("capacityFillPercentage").GetDecimal());
            Assert.Equal(4.5, root.GetProperty("averageRating").GetDouble());
            Assert.Equal(2, root.GetProperty("reviewCount").GetInt32());

            var distribution = root.GetProperty("ratingDistribution").EnumerateArray().ToArray();
            Assert.Equal(5, distribution.Length);
            Assert.Equal(0, distribution[0].GetProperty("count").GetInt32());
            Assert.Equal(1, distribution[3].GetProperty("count").GetInt32());
            Assert.Equal(1, distribution[4].GetProperty("count").GetInt32());

            var recentReviews = root.GetProperty("recentReviews").EnumerateArray().ToArray();
            Assert.Equal(2, recentReviews.Length);
            Assert.Equal("Useful event.", recentReviews[0].GetProperty("comment").GetString());
        }

        [Fact]
        public async Task GetById_ForOtherOrganizer_ReturnsNotFound()
        {
            var eventId = await SeedEventAsync(Guid.NewGuid(), "Hidden insight event");
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Organizer);

            var response = await client.GetAsync($"/api/insights/events/{eventId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private async Task<Guid> SeedEventWithInsightDataAsync(Guid organizerUserId)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Users.Add(CreateUser(organizerUserId, "Insight Organizer"));

            var startsAtUtc = DateTime.UtcNow.AddDays(-4);
            var eventItem = Event.Create(
                $"Insight event {Guid.NewGuid():N}",
                "Event used by insight endpoint tests.",
                startsAtUtc,
                startsAtUtc.AddHours(3),
                5,
                1000m,
                "IT",
                1,
                organizerUserId,
                DateTime.UtcNow.AddDays(-10));
            eventItem.Publish(DateTime.UtcNow.AddDays(-6));
            eventItem.Complete(DateTime.UtcNow.AddDays(-1));
            dbContext.Events.Add(eventItem);

            var pendingUserId = Guid.NewGuid();
            var confirmedUserId = Guid.NewGuid();
            var secondConfirmedUserId = Guid.NewGuid();
            var rejectedUserId = Guid.NewGuid();
            var cancelledUserId = Guid.NewGuid();
            var participantUserIds = new[]
            {
                pendingUserId,
                confirmedUserId,
                secondConfirmedUserId,
                rejectedUserId,
                cancelledUserId,
            };

            foreach (var participantUserId in participantUserIds)
            {
                dbContext.Users.Add(CreateUser(participantUserId, "Insight Participant"));
            }

            var pendingRegistration = Registration.Create(eventItem.Id, pendingUserId, DateTime.UtcNow.AddDays(-4));
            var confirmedRegistration = Registration.Create(
                eventItem.Id,
                confirmedUserId,
                DateTime.UtcNow.AddDays(-4));
            confirmedRegistration.Confirm(organizerUserId, DateTime.UtcNow.AddDays(-3));
            var secondConfirmedRegistration = Registration.Create(
                eventItem.Id,
                secondConfirmedUserId,
                DateTime.UtcNow.AddDays(-4));
            secondConfirmedRegistration.Confirm(organizerUserId, DateTime.UtcNow.AddDays(-3));
            var rejectedRegistration = Registration.Create(
                eventItem.Id,
                rejectedUserId,
                DateTime.UtcNow.AddDays(-4));
            rejectedRegistration.Reject("No capacity.", organizerUserId, DateTime.UtcNow.AddDays(-3));
            var cancelledRegistration = Registration.Create(
                eventItem.Id,
                cancelledUserId,
                DateTime.UtcNow.AddDays(-4));
            cancelledRegistration.Cancel(DateTime.UtcNow.AddDays(-3));

            dbContext.Registrations.AddRange(
                pendingRegistration,
                confirmedRegistration,
                secondConfirmedRegistration,
                rejectedRegistration,
                cancelledRegistration);
            dbContext.Reviews.Add(Review.Create(
                eventItem.Id,
                confirmedUserId,
                5,
                "Great event.",
                DateTime.UtcNow.AddDays(-2)));
            dbContext.Reviews.Add(Review.Create(
                eventItem.Id,
                secondConfirmedUserId,
                4,
                "Useful event.",
                DateTime.UtcNow.AddDays(-1)));

            await dbContext.SaveChangesAsync();
            return eventItem.Id;
        }

        private async Task<Guid> SeedEventAsync(Guid organizerUserId, string title)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Users.Add(CreateUser(organizerUserId, "Insight Organizer"));
            var startsAtUtc = DateTime.UtcNow.AddDays(5);
            var eventItem = Event.Create(
                $"{title} {Guid.NewGuid():N}",
                "Event used by insight endpoint tests.",
                startsAtUtc,
                startsAtUtc.AddHours(3),
                10,
                1000m,
                "IT",
                1,
                organizerUserId,
                DateTime.UtcNow);

            dbContext.Events.Add(eventItem);
            await dbContext.SaveChangesAsync();
            return eventItem.Id;
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

        private HttpClient CreateAuthenticatedClient(Guid userId, params string[] roles)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, string.Join(',', roles));
            return client;
        }
    }
}
