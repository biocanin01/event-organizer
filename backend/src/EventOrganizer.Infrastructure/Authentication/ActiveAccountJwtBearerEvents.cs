using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace EventOrganizer.Infrastructure.Authentication
{
    public sealed class ActiveAccountJwtBearerEvents : JwtBearerEvents
    {
        private readonly IIdentityService _identityService;

        public ActiveAccountJwtBearerEvents(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public override async Task TokenValidated(TokenValidatedContext context)
        {
            var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdValue, out var userId))
            {
                context.Fail("Access token does not contain a valid user identifier.");
                return;
            }

            var user = await _identityService.FindByIdAsync(
                userId,
                context.HttpContext.RequestAborted);

            if (user is null || user.Status != UserStatus.Active)
            {
                context.Fail("User account is not active.");
            }
        }
    }
}
