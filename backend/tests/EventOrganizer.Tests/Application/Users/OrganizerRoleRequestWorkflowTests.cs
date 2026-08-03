using EventOrganizer.Application.Commands.ApproveOrganizerRoleRequest;
using EventOrganizer.Application.Commands.RejectOrganizerRoleRequest;
using EventOrganizer.Application.Commands.SubmitOrganizerRoleRequest;
using EventOrganizer.Application.Commands.WithdrawOrganizerRoleRequest;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Queries.GetMyOrganizerRoleRequest;
using EventOrganizer.Application.Queries.ListOrganizerRoleRequests;
using EventOrganizer.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Users
{
    public sealed class OrganizerRoleRequestWorkflowTests : ApplicationTestBase
    {
        [Fact]
        public async Task Submit_WithActiveParticipant_CreatesPendingRequest()
        {
            var userId = await CreateOrganizerUserAsync();
            var identityService = new FakeIdentityService(
                new AuthUserResult(
                    userId,
                    "Test Participant",
                    "participant@example.com",
                    UserStatus.Active),
                ApplicationRoles.Participant);
            var handler = new SubmitOrganizerRoleRequestCommandHandler(
                DbContext,
                new TestCurrentUserService(userId, ApplicationRoles.Participant),
                identityService);

            var requestId = await handler.Handle(
                new SubmitOrganizerRoleRequestCommand("I want to organize academic events."),
                CancellationToken.None);

            var organizerRoleRequest = await DbContext.OrganizerRoleRequests
                .SingleAsync(request => request.Id == requestId);

            Assert.Equal(userId, organizerRoleRequest.UserId);
            Assert.Equal(OrganizerRoleRequestStatus.Pending, organizerRoleRequest.Status);
            Assert.Equal(1, organizerRoleRequest.Version);
        }

        [Fact]
        public async Task Submit_WhenUserAlreadyOrganizer_ThrowsConflictException()
        {
            var userId = await CreateOrganizerUserAsync();
            var identityService = new FakeIdentityService(
                new AuthUserResult(
                    userId,
                    "Test Organizer",
                    "organizer@example.com",
                    UserStatus.Active),
                ApplicationRoles.Participant,
                ApplicationRoles.Organizer);
            var handler = new SubmitOrganizerRoleRequestCommandHandler(
                DbContext,
                new TestCurrentUserService(userId, ApplicationRoles.Organizer),
                identityService);

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(
                    new SubmitOrganizerRoleRequestCommand("I already organize events."),
                    CancellationToken.None));
        }

        [Theory]
        [InlineData(OrganizerRoleRequestStatus.Rejected)]
        [InlineData(OrganizerRoleRequestStatus.Withdrawn)]
        public async Task Submit_AfterPreviousRequestIsClosed_CreatesNewPendingRequest(
            OrganizerRoleRequestStatus previousStatus)
        {
            var participantUserId = await CreateOrganizerUserAsync("participant@example.com");
            var previousRequest = OrganizerRoleRequest.Create(
                participantUserId,
                "Previous request.",
                new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc));

            if (previousStatus == OrganizerRoleRequestStatus.Rejected)
            {
                var adminUserId = await CreateOrganizerUserAsync("admin@example.com");
                previousRequest.Reject(
                    adminUserId,
                    "Additional information is required.",
                    new DateTime(2026, 8, 1, 11, 0, 0, DateTimeKind.Utc));
            }
            else
            {
                previousRequest.Withdraw(
                    new DateTime(2026, 8, 1, 11, 0, 0, DateTimeKind.Utc));
            }

            DbContext.OrganizerRoleRequests.Add(previousRequest);
            await DbContext.SaveChangesAsync();

            var identityService = new FakeIdentityService(
                new AuthUserResult(
                    participantUserId,
                    "Test Participant",
                    "participant@example.com",
                    UserStatus.Active),
                ApplicationRoles.Participant);
            var handler = new SubmitOrganizerRoleRequestCommandHandler(
                DbContext,
                new TestCurrentUserService(participantUserId, ApplicationRoles.Participant),
                identityService);

            var newRequestId = await handler.Handle(
                new SubmitOrganizerRoleRequestCommand("Updated organizer request."),
                CancellationToken.None);

            var newRequest = await DbContext.OrganizerRoleRequests
                .SingleAsync(request => request.Id == newRequestId);

            Assert.Equal(OrganizerRoleRequestStatus.Pending, newRequest.Status);
            Assert.NotEqual(previousRequest.Id, newRequest.Id);
        }

        [Fact]
        public async Task Approve_WithExpectedVersion_ApprovesRequestAndAssignsOrganizerRole()
        {
            var participantUserId = await CreateOrganizerUserAsync("participant@example.com");
            var adminUserId = await CreateOrganizerUserAsync("admin@example.com");
            var organizerRoleRequest = OrganizerRoleRequest.Create(
                participantUserId,
                "I want to organize professional events.",
                DateTime.UtcNow);
            DbContext.OrganizerRoleRequests.Add(organizerRoleRequest);
            await DbContext.SaveChangesAsync();

            var identityService = new FakeIdentityService(
                new AuthUserResult(
                    participantUserId,
                    "Test Participant",
                    "participant@example.com",
                    UserStatus.Active),
                ApplicationRoles.Participant);
            var handler = new ApproveOrganizerRoleRequestCommandHandler(
                DbContext,
                new TestCurrentUserService(adminUserId, ApplicationRoles.Admin),
                identityService);

            await handler.Handle(
                new ApproveOrganizerRoleRequestCommand(
                    organizerRoleRequest.Id,
                    organizerRoleRequest.Version),
                CancellationToken.None);

            Assert.Equal(OrganizerRoleRequestStatus.Approved, organizerRoleRequest.Status);
            Assert.Equal(adminUserId, organizerRoleRequest.ReviewedByAdminUserId);
            Assert.Contains(
                identityService.AssignedRoles,
                assignedRole =>
                    assignedRole.UserId == participantUserId
                    && assignedRole.Role == ApplicationRoles.Organizer);
        }

        [Fact]
        public async Task Approve_WithStaleVersion_ThrowsConflictException()
        {
            var participantUserId = await CreateOrganizerUserAsync("participant@example.com");
            var adminUserId = await CreateOrganizerUserAsync("admin@example.com");
            var organizerRoleRequest = OrganizerRoleRequest.Create(
                participantUserId,
                "I want to organize professional events.",
                DateTime.UtcNow);
            DbContext.OrganizerRoleRequests.Add(organizerRoleRequest);
            await DbContext.SaveChangesAsync();

            var identityService = new FakeIdentityService(
                new AuthUserResult(
                    participantUserId,
                    "Test Participant",
                    "participant@example.com",
                    UserStatus.Active),
                ApplicationRoles.Participant);
            var handler = new ApproveOrganizerRoleRequestCommandHandler(
                DbContext,
                new TestCurrentUserService(adminUserId, ApplicationRoles.Admin),
                identityService);

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(
                    new ApproveOrganizerRoleRequestCommand(
                        organizerRoleRequest.Id,
                        organizerRoleRequest.Version + 1),
                    CancellationToken.None));

            Assert.Empty(identityService.AssignedRoles);
        }

        [Fact]
        public async Task Reject_WithReason_RejectsRequest()
        {
            var participantUserId = await CreateOrganizerUserAsync("participant@example.com");
            var adminUserId = await CreateOrganizerUserAsync("admin@example.com");
            var organizerRoleRequest = OrganizerRoleRequest.Create(
                participantUserId,
                "I want to organize professional events.",
                DateTime.UtcNow);
            DbContext.OrganizerRoleRequests.Add(organizerRoleRequest);
            await DbContext.SaveChangesAsync();

            var handler = new RejectOrganizerRoleRequestCommandHandler(
                DbContext,
                new TestCurrentUserService(adminUserId, ApplicationRoles.Admin));

            await handler.Handle(
                new RejectOrganizerRoleRequestCommand(
                    organizerRoleRequest.Id,
                    "Please provide more details about planned events.",
                    organizerRoleRequest.Version),
                CancellationToken.None);

            Assert.Equal(OrganizerRoleRequestStatus.Rejected, organizerRoleRequest.Status);
            Assert.Equal("Please provide more details about planned events.", organizerRoleRequest.DecisionReason);
            Assert.Equal(2, organizerRoleRequest.Version);
        }

        [Fact]
        public async Task Withdraw_ByOwner_WithdrawsPendingRequest()
        {
            var participantUserId = await CreateOrganizerUserAsync("participant@example.com");
            var organizerRoleRequest = OrganizerRoleRequest.Create(
                participantUserId,
                "I want to organize professional events.",
                DateTime.UtcNow);
            DbContext.OrganizerRoleRequests.Add(organizerRoleRequest);
            await DbContext.SaveChangesAsync();

            var handler = new WithdrawOrganizerRoleRequestCommandHandler(
                DbContext,
                new TestCurrentUserService(participantUserId, ApplicationRoles.Participant));

            await handler.Handle(
                new WithdrawOrganizerRoleRequestCommand(
                    organizerRoleRequest.Id,
                    organizerRoleRequest.Version),
                CancellationToken.None);

            Assert.Equal(OrganizerRoleRequestStatus.Withdrawn, organizerRoleRequest.Status);
            Assert.NotNull(organizerRoleRequest.WithdrawnAtUtc);
            Assert.Equal(2, organizerRoleRequest.Version);
        }

        [Fact]
        public async Task GetMyRequest_ReturnsLatestUserRequest()
        {
            var participantUserId = await CreateOrganizerUserAsync("participant@example.com");
            var oldRequest = OrganizerRoleRequest.Create(
                participantUserId,
                "Old request.",
                new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc));
            oldRequest.Withdraw(new DateTime(2026, 8, 1, 11, 0, 0, DateTimeKind.Utc));
            var latestRequest = OrganizerRoleRequest.Create(
                participantUserId,
                "Latest request.",
                new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc));
            DbContext.OrganizerRoleRequests.AddRange(oldRequest, latestRequest);
            await DbContext.SaveChangesAsync();

            var handler = new GetMyOrganizerRoleRequestQueryHandler(
                DbContext,
                new TestCurrentUserService(participantUserId, ApplicationRoles.Participant));

            var result = await handler.Handle(
                new GetMyOrganizerRoleRequestQuery(),
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(latestRequest.Id, result.Id);
            Assert.Equal("Pending", result.Status);
        }

        [Fact]
        public async Task List_DefaultsToPendingRequests()
        {
            var firstUserId = await CreateOrganizerUserAsync("first@example.com");
            var secondUserId = await CreateOrganizerUserAsync("second@example.com");
            var pendingRequest = OrganizerRoleRequest.Create(
                firstUserId,
                "Pending request.",
                DateTime.UtcNow);
            var withdrawnRequest = OrganizerRoleRequest.Create(
                secondUserId,
                "Withdrawn request.",
                DateTime.UtcNow);
            withdrawnRequest.Withdraw(DateTime.UtcNow);
            DbContext.OrganizerRoleRequests.AddRange(pendingRequest, withdrawnRequest);
            await DbContext.SaveChangesAsync();

            var handler = new ListOrganizerRoleRequestsQueryHandler(DbContext);

            var result = await handler.Handle(
                new ListOrganizerRoleRequestsQuery(null),
                CancellationToken.None);

            Assert.Single(result);
            Assert.Equal(pendingRequest.Id, result[0].Id);
        }

        private sealed class FakeIdentityService : IIdentityService
        {
            private readonly AuthUserResult _user;
            private readonly List<string> _roles;

            public FakeIdentityService(AuthUserResult user, params string[] roles)
            {
                _user = user;
                _roles = roles.ToList();
            }

            public List<(Guid UserId, string Role)> AssignedRoles { get; } = [];

            public Task<AuthUserResult?> FindByEmailAsync(
                string email,
                CancellationToken cancellationToken)
            {
                return Task.FromResult<AuthUserResult?>(null);
            }

            public Task<AuthUserResult> CreateUserAsync(
                string fullName,
                string email,
                string password,
                CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<bool> CheckPasswordAsync(
                Guid userId,
                string password,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(userId == _user.UserId);
            }

            public Task<IReadOnlyCollection<string>> GetRolesAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                if (userId != _user.UserId)
                {
                    return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
                }

                return Task.FromResult<IReadOnlyCollection<string>>(_roles.ToArray());
            }

            public Task AddToRoleAsync(
                Guid userId,
                string role,
                CancellationToken cancellationToken)
            {
                if (!_roles.Contains(role))
                {
                    _roles.Add(role);
                    AssignedRoles.Add((userId, role));
                }

                return Task.CompletedTask;
            }

            public Task<AuthUserResult?> FindByIdAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                return Task.FromResult<AuthUserResult?>(
                    userId == _user.UserId ? _user : null);
            }
        }

        private sealed class TestCurrentUserService : ICurrentUserService
        {
            private readonly string[] _roles;

            public TestCurrentUserService(Guid? userId, params string[] roles)
            {
                UserId = userId;
                _roles = roles;
            }

            public Guid? UserId { get; }

            public string? Email => null;

            public bool IsAuthenticated => UserId.HasValue;

            public IReadOnlyCollection<string> Roles => _roles;

            public bool IsInRole(string role)
            {
                return _roles.Contains(role);
            }
        }
    }
}
