using EventOrganizer.Api.Contracts.Registrations;
using EventOrganizer.Api.Contracts.Reviews;
using EventOrganizer.Application.Common.Constants;
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
    public sealed class ParticipationReviewInsightWorkflowEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ParticipationReviewInsightWorkflowEndpointTests(
            CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ConfirmedParticipant_CanReviewCompletedEvent_AndReviewAppearsInInsights()
        {
            var (eventId, organizerUserId, participantUserId) = await SeedPublishedEventAsync();
            var participantClient = CreateAuthenticatedClient(
                participantUserId,
                ApplicationRoles.Participant);
            var organizerClient = CreateAuthenticatedClient(
                organizerUserId,
                ApplicationRoles.Organizer);

            var createRegistrationResponse = await participantClient.PostAsync(
                $"/api/events/{eventId}/registrations",
                null);
            Assert.Equal(HttpStatusCode.Created, createRegistrationResponse.StatusCode);

            using var createdRegistration = JsonDocument.Parse(
                await createRegistrationResponse.Content.ReadAsStringAsync());
            var registrationId = createdRegistration.RootElement.GetProperty("id").GetGuid();
            var registrationVersion = createdRegistration.RootElement.GetProperty("version").GetInt32();

            var confirmResponse = await organizerClient.PatchAsJsonAsync(
                $"/api/registrations/{registrationId}/confirm",
                new RegistrationVersionRequest(registrationVersion));
            Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

            await MoveEventToPastAsync(eventId);

            var completeResponse = await organizerClient.PatchAsync(
                $"/api/events/{eventId}/complete",
                null);
            Assert.Equal(HttpStatusCode.NoContent, completeResponse.StatusCode);

            var createReviewResponse = await participantClient.PostAsJsonAsync(
                $"/api/events/{eventId}/reviews",
                new CreateReviewRequest(5, "Odličan događaj."));
            Assert.Equal(HttpStatusCode.Created, createReviewResponse.StatusCode);

            var insightResponse = await organizerClient.GetAsync(
                $"/api/insights/events/{eventId}");
            Assert.Equal(HttpStatusCode.OK, insightResponse.StatusCode);

            using var insight = JsonDocument.Parse(await insightResponse.Content.ReadAsStringAsync());
            var root = insight.RootElement;
            Assert.Equal(1, root.GetProperty("confirmedRegistrationCount").GetInt32());
            Assert.Equal(10m, root.GetProperty("capacityFillPercentage").GetDecimal());
            Assert.Equal(1, root.GetProperty("reviewCount").GetInt32());
            Assert.Equal(5d, root.GetProperty("averageRating").GetDouble());

            var ratingDistribution = root
                .GetProperty("ratingDistribution")
                .EnumerateArray()
                .ToArray();
            Assert.Equal(1, ratingDistribution[4].GetProperty("count").GetInt32());

            var recentReview = Assert.Single(
                root.GetProperty("recentReviews").EnumerateArray());
            Assert.Equal("Odličan događaj.", recentReview.GetProperty("comment").GetString());
        }

        private async Task<(Guid EventId, Guid OrganizerUserId, Guid ParticipantUserId)>
            SeedPublishedEventAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var organizerUserId = Guid.NewGuid();
            var participantUserId = Guid.NewGuid();
            var startsAtUtc = DateTime.UtcNow.AddDays(5);
            var eventItem = Event.Create(
                $"Participation workflow {Guid.NewGuid():N}",
                "Event used by the participation workflow endpoint test.",
                startsAtUtc,
                startsAtUtc.AddHours(3),
                10,
                1000m,
                "IT",
                1,
                organizerUserId,
                DateTime.UtcNow);
            eventItem.Publish(DateTime.UtcNow);

            dbContext.Users.Add(CreateUser(organizerUserId, "Workflow Organizer"));
            dbContext.Users.Add(CreateUser(participantUserId, "Workflow Participant"));
            dbContext.Events.Add(eventItem);
            await dbContext.SaveChangesAsync();

            return (eventItem.Id, organizerUserId, participantUserId);
        }

        private async Task MoveEventToPastAsync(Guid eventId)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var startsAtUtc = DateTime.UtcNow.AddHours(-4);

            await dbContext.Events
                .Where(eventItem => eventItem.Id == eventId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(eventItem => eventItem.StartsAtUtc, startsAtUtc)
                    .SetProperty(eventItem => eventItem.EndsAtUtc, startsAtUtc.AddHours(3)));
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
