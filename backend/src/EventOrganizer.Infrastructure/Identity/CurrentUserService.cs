using EventOrganizer.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EventOrganizer.Infrastructure.Identity
{
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);

                return Guid.TryParse(userId, out var parsedUserId)
                    ? parsedUserId
                    : null;
            }
        }

        public string? Email => User?.FindFirstValue(ClaimTypes.Email);

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        public IReadOnlyCollection<string> Roles =>
            User?.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToArray()
            ?? Array.Empty<string>();

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public bool IsInRole(string role)
        {
            return User?.IsInRole(role) == true;
        }
    }
}
