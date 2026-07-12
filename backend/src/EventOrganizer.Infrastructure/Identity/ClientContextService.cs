using EventOrganizer.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace EventOrganizer.Infrastructure.Identity
{
    public sealed class ClientContextService : IClientContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClientContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? IpAddress =>
            _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}
