using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Users;
using EventOrganizer.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EventOrganizer.Tests.Infrastructure.Authentication
{
    public sealed class ActiveAccountJwtBearerEventsTests
    {
        [Fact]
        public async Task TokenValidated_WithActiveUser_KeepsAuthenticationSuccessful()
        {
            var userId = Guid.NewGuid();
            var events = new ActiveAccountJwtBearerEvents(
                new FakeIdentityService(CreateUser(userId, UserStatus.Active)));
            var context = CreateContext(userId);

            await events.TokenValidated(context);

            Assert.Null(context.Result?.Failure);
        }

        [Fact]
        public async Task TokenValidated_WithSuspendedUser_FailsAuthentication()
        {
            var userId = Guid.NewGuid();
            var events = new ActiveAccountJwtBearerEvents(
                new FakeIdentityService(CreateUser(userId, UserStatus.Suspended)));
            var context = CreateContext(userId);

            await events.TokenValidated(context);

            Assert.NotNull(context.Result?.Failure);
        }

        private static TokenValidatedContext CreateContext(Guid userId)
        {
            var httpContext = new DefaultHttpContext();
            var scheme = new AuthenticationScheme(
                JwtBearerDefaults.AuthenticationScheme,
                JwtBearerDefaults.AuthenticationScheme,
                typeof(JwtBearerHandler));
            var context = new TokenValidatedContext(
                httpContext,
                scheme,
                new JwtBearerOptions());
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                JwtBearerDefaults.AuthenticationScheme);
            context.Principal = new ClaimsPrincipal(identity);

            return context;
        }

        private static AuthUserResult CreateUser(Guid userId, UserStatus status)
        {
            return new AuthUserResult(
                userId,
                "Test User",
                "user@example.com",
                status);
        }

        private sealed class FakeIdentityService : IIdentityService
        {
            private readonly AuthUserResult _user;

            public FakeIdentityService(AuthUserResult user)
            {
                _user = user;
            }

            public Task<AuthUserResult?> FindByIdAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                return Task.FromResult<AuthUserResult?>(
                    userId == _user.UserId ? _user : null);
            }

            public Task<AuthUserResult?> FindByEmailAsync(
                string email,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<AuthUserResult> CreateUserAsync(
                string fullName,
                string email,
                string password,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<bool> CheckPasswordAsync(
                Guid userId,
                string password,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<IReadOnlyCollection<string>> GetRolesAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task AddToRoleAsync(
                Guid userId,
                string role,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }
        }
    }
}
