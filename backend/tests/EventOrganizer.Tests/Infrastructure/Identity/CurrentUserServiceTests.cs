using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EventOrganizer.Tests.Infrastructure.Identity
{
    public sealed class CurrentUserServiceTests
    {
        [Fact]
        public void IsAuthenticated_WhenHttpContextIsMissing_ReturnsFalse()
        {
            var httpContextAccessor = new HttpContextAccessor();
            var currentUserService = new CurrentUserService(httpContextAccessor);

            Assert.False(currentUserService.IsAuthenticated);
            Assert.Null(currentUserService.UserId);
            Assert.Null(currentUserService.Email);
            Assert.Empty(currentUserService.Roles);
        }

        [Fact]
        public void IsAuthenticated_WhenIdentityIsNotAuthenticated_ReturnsFalse()
        {
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()),
                },
            };

            var currentUserService = new CurrentUserService(httpContextAccessor);

            Assert.False(currentUserService.IsAuthenticated);
        }

        [Fact]
        public void CurrentUserProperties_WhenClaimsExist_ReturnExpectedValues()
        {
            var userId = Guid.NewGuid();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, "test.user@example.com"),
                new Claim(ClaimTypes.Role, ApplicationRoles.Participant),
                new Claim(ClaimTypes.Role, ApplicationRoles.Organizer),
            };

            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")),
                },
            };

            var currentUserService = new CurrentUserService(httpContextAccessor);

            Assert.True(currentUserService.IsAuthenticated);
            Assert.Equal(userId, currentUserService.UserId);
            Assert.Equal("test.user@example.com", currentUserService.Email);
            Assert.Contains(ApplicationRoles.Participant, currentUserService.Roles);
            Assert.Contains(ApplicationRoles.Organizer, currentUserService.Roles);
            Assert.True(currentUserService.IsInRole(ApplicationRoles.Participant));
            Assert.False(currentUserService.IsInRole(ApplicationRoles.Admin));
        }

        [Fact]
        public void UserId_WhenNameIdentifierIsInvalid_ReturnsNull()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "not-a-guid"),
            };

            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")),
                },
            };

            var currentUserService = new CurrentUserService(httpContextAccessor);

            Assert.Null(currentUserService.UserId);
        }
    }
}
