using EventOrganizer.Api.Contracts.OrganizerRoleRequests;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Users;
using EventOrganizer.Infrastructure.Identity;
using EventOrganizer.Infrastructure.Persistance;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace EventOrganizer.Tests.Api
{
    public sealed class OrganizerRoleRequestEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public OrganizerRoleRequestEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Submit_WithoutAuthentication_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync(
                "/api/organizer-role-requests",
                new SubmitOrganizerRoleRequestRequest("I want to organize events."));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Submit_WithParticipantRole_ReturnsCreated()
        {
            var participantUserId = await CreateUserWithRolesAsync(ApplicationRoles.Participant);
            var client = CreateAuthenticatedClient(
                participantUserId,
                ApplicationRoles.Participant);

            var response = await client.PostAsJsonAsync(
                "/api/organizer-role-requests",
                new SubmitOrganizerRoleRequestRequest("I want to organize events."));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task List_WithParticipantRole_ReturnsForbidden()
        {
            var participantUserId = await CreateUserWithRolesAsync(ApplicationRoles.Participant);
            var client = CreateAuthenticatedClient(
                participantUserId,
                ApplicationRoles.Participant);

            var response = await client.GetAsync("/api/organizer-role-requests");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task List_WithAdminRole_ReturnsPendingRequests()
        {
            var participantUserId = await CreateUserWithRolesAsync(ApplicationRoles.Participant);
            var adminUserId = await CreateUserWithRolesAsync(ApplicationRoles.Admin);
            var request = OrganizerRoleRequest.Create(
                participantUserId,
                "I want to organize events.",
                DateTime.UtcNow);

            await AddOrganizerRoleRequestAsync(request);

            var client = CreateAuthenticatedClient(adminUserId, ApplicationRoles.Admin);

            var response = await client.GetAsync("/api/organizer-role-requests");
            var requests = await response.Content.ReadFromJsonAsync<List<OrganizerRoleRequestResponse>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(requests);
            Assert.Contains(requests, item => item.Id == request.Id);
        }

        [Fact]
        public async Task Approve_WithAdminRole_AssignsOrganizerRole()
        {
            var participantUserId = await CreateUserWithRolesAsync(ApplicationRoles.Participant);
            var adminUserId = await CreateUserWithRolesAsync(ApplicationRoles.Admin);
            var request = OrganizerRoleRequest.Create(
                participantUserId,
                "I want to organize events.",
                DateTime.UtcNow);

            await AddOrganizerRoleRequestAsync(request);

            var client = CreateAuthenticatedClient(adminUserId, ApplicationRoles.Admin);

            var response = await client.PatchAsJsonAsync(
                $"/api/organizer-role-requests/{request.Id}/approve",
                new ApproveOrganizerRoleRequestRequest(request.Version));

            var roles = await GetUserRolesAsync(participantUserId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Contains(ApplicationRoles.Organizer, roles);
        }

        private HttpClient CreateAuthenticatedClient(Guid userId, string role)
        {
            var client = _factory.CreateClient();

            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);

            return client;
        }

        private async Task<Guid> CreateUserWithRolesAsync(params string[] roles)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var email = $"{Guid.NewGuid():N}@example.com";
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FullName = "Test User",
                Status = UserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
                VerifiedAtUtc = DateTime.UtcNow,
            };

            var createResult = await userManager.CreateAsync(user);
            Assert.True(createResult.Succeeded);

            foreach (var role in roles)
            {
                var roleResult = await userManager.AddToRoleAsync(user, role);
                Assert.True(roleResult.Succeeded);
            }

            return user.Id;
        }

        private async Task AddOrganizerRoleRequestAsync(OrganizerRoleRequest request)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.OrganizerRoleRequests.Add(request);
            await dbContext.SaveChangesAsync();
        }

        private async Task<IReadOnlyCollection<string>> GetUserRolesAsync(Guid userId)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId.ToString());

            Assert.NotNull(user);

            return (await userManager.GetRolesAsync(user)).ToArray();
        }
    }
}
