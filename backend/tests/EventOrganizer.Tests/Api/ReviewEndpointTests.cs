using EventOrganizer.Api.Contracts.Reviews;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Registrations;
using EventOrganizer.Domain.Reviews;
using EventOrganizer.Domain.Users;
using EventOrganizer.Infrastructure.Identity;
using EventOrganizer.Infrastructure.Persistance;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventOrganizer.Tests.Api
{
    public sealed class ReviewEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ReviewEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Create_WithConfirmedRegistrationForCompletedEvent_ReturnsCreatedReview()
        {
            var (eventId, _, participantUserId) = await SeedCompletedEventAsync();
            var client = CreateAuthenticatedClient(participantUserId, ApplicationRoles.Participant);

            var response = await client.PostAsJsonAsync(
                $"/api/events/{eventId}/reviews",
                new CreateReviewRequest(5, "Odlican dogadjaj."));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(5, payload.RootElement.GetProperty("rating").GetInt32());
            Assert.Equal("Odlican dogadjaj.", payload.RootElement.GetProperty("comment").GetString());
            Assert.Equal(1, payload.RootElement.GetProperty("version").GetInt32());
        }

        [Fact]
        public async Task Create_WhenEventIsNotCompleted_ReturnsConflict()
        {
            var (eventId, _, participantUserId) = await SeedCompletedEventAsync(complete: false);
            var client = CreateAuthenticatedClient(participantUserId, ApplicationRoles.Participant);

            var response = await client.PostAsJsonAsync(
                $"/api/events/{eventId}/reviews",
                new CreateReviewRequest(5, "Odlican dogadjaj."));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Create_WithoutConfirmedRegistration_ReturnsForbidden()
        {
            var (eventId, _, _) = await SeedCompletedEventAsync();
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Participant);

            var response = await client.PostAsJsonAsync(
                $"/api/events/{eventId}/reviews",
                new CreateReviewRequest(5, "Odlican dogadjaj."));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenReviewAlreadyExists_ReturnsConflict()
        {
            var (eventId, _, participantUserId) = await SeedCompletedEventAsync();
            var client = CreateAuthenticatedClient(participantUserId, ApplicationRoles.Participant);
            await client.PostAsJsonAsync(
                $"/api/events/{eventId}/reviews",
                new CreateReviewRequest(5, "Odlican dogadjaj."));

            var response = await client.PostAsJsonAsync(
                $"/api/events/{eventId}/reviews",
                new CreateReviewRequest(4, "Drugi komentar."));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Update_ByOwner_UpdatesReview()
        {
            var (eventId, _, participantUserId) = await SeedCompletedEventAsync();
            var reviewId = await SeedReviewAsync(eventId, participantUserId);
            var client = CreateAuthenticatedClient(participantUserId, ApplicationRoles.Participant);

            var response = await client.PutAsJsonAsync(
                $"/api/reviews/{reviewId}",
                new UpdateReviewRequest(1, 4, "Vrlo korisno."));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(4, payload.RootElement.GetProperty("rating").GetInt32());
            Assert.Equal("Vrlo korisno.", payload.RootElement.GetProperty("comment").GetString());
            Assert.Equal(2, payload.RootElement.GetProperty("version").GetInt32());
        }

        [Fact]
        public async Task Update_WithStaleVersion_ReturnsConflict()
        {
            var (eventId, _, participantUserId) = await SeedCompletedEventAsync();
            var reviewId = await SeedReviewAsync(eventId, participantUserId);
            var client = CreateAuthenticatedClient(participantUserId, ApplicationRoles.Participant);

            var response = await client.PutAsJsonAsync(
                $"/api/reviews/{reviewId}",
                new UpdateReviewRequest(999, 4, "Vrlo korisno."));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Update_ByOtherUser_ReturnsForbidden()
        {
            var (eventId, _, participantUserId) = await SeedCompletedEventAsync();
            var reviewId = await SeedReviewAsync(eventId, participantUserId);
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Participant);

            var response = await client.PutAsJsonAsync(
                $"/api/reviews/{reviewId}",
                new UpdateReviewRequest(1, 4, "Vrlo korisno."));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ListForEvent_ReturnsPublicReviews()
        {
            var (eventId, _, participantUserId) = await SeedCompletedEventAsync();
            await SeedReviewAsync(eventId, participantUserId);
            var client = _factory.CreateClient();

            var response = await client.GetAsync($"/api/events/{eventId}/reviews");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Contains(
                payload.RootElement.EnumerateArray(),
                review => review.GetProperty("eventId").GetGuid() == eventId);
        }

        [Fact]
        public async Task ListManaged_ForEventOwner_ReturnsOwnedEventReviews()
        {
            var (eventId, organizerUserId, participantUserId) = await SeedCompletedEventAsync();
            await SeedReviewAsync(eventId, participantUserId);
            var client = CreateAuthenticatedClient(organizerUserId, ApplicationRoles.Organizer);

            var response = await client.GetAsync($"/api/reviews/manage?eventId={eventId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Contains(
                payload.RootElement.EnumerateArray(),
                review => review.GetProperty("eventId").GetGuid() == eventId);
        }

        [Fact]
        public async Task ListManaged_ForOtherOrganizer_ReturnsForbidden()
        {
            var (eventId, _, participantUserId) = await SeedCompletedEventAsync();
            await SeedReviewAsync(eventId, participantUserId);
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Organizer);

            var response = await client.GetAsync($"/api/reviews/manage?eventId={eventId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ListManaged_WithoutEventId_ForAdmin_ReturnsAllReviews()
        {
            var (eventId, _, participantUserId) = await SeedCompletedEventAsync();
            await SeedReviewAsync(eventId, participantUserId);
            var client = CreateAuthenticatedClient(Guid.NewGuid(), ApplicationRoles.Admin);

            var response = await client.GetAsync("/api/reviews/manage");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Contains(
                payload.RootElement.EnumerateArray(),
                review => review.GetProperty("eventId").GetGuid() == eventId);
        }

        private async Task<(Guid EventId, Guid OrganizerUserId, Guid ParticipantUserId)> SeedCompletedEventAsync(
            bool complete = true)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var organizerUserId = Guid.NewGuid();
            var participantUserId = Guid.NewGuid();
            var startsAtUtc = DateTime.UtcNow.AddDays(-2);
            var eventItem = Event.Create(
                $"Review event {Guid.NewGuid():N}",
                "Event used by review endpoint tests.",
                startsAtUtc,
                startsAtUtc.AddHours(3),
                20,
                1000m,
                "IT",
                1,
                organizerUserId,
                DateTime.UtcNow.AddDays(-10));
            eventItem.Publish(DateTime.UtcNow.AddDays(-1));
            if (complete)
            {
                eventItem.Complete(DateTime.UtcNow);
            }

            var registration = Registration.Create(eventItem.Id, participantUserId, DateTime.UtcNow.AddDays(-2));
            registration.Confirm(organizerUserId, DateTime.UtcNow.AddDays(-1));

            dbContext.Users.Add(CreateUser(organizerUserId, "Organizer"));
            dbContext.Users.Add(CreateUser(participantUserId, "Participant"));
            dbContext.Events.Add(eventItem);
            dbContext.Registrations.Add(registration);
            await dbContext.SaveChangesAsync();
            return (eventItem.Id, organizerUserId, participantUserId);
        }

        private async Task<Guid> SeedReviewAsync(Guid eventId, Guid participantUserId)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var review = Review.Create(
                eventId,
                participantUserId,
                5,
                "Odlican dogadjaj.",
                DateTime.UtcNow);
            dbContext.Reviews.Add(review);
            await dbContext.SaveChangesAsync();
            return review.Id;
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
