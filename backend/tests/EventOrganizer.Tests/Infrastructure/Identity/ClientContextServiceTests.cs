using EventOrganizer.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace EventOrganizer.Tests.Infrastructure.Identity
{
    public sealed class ClientContextServiceTests
    {
        [Fact]
        public void IpAddress_WhenHttpContextIsMissing_ReturnsNull()
        {
            var httpContextAccessor = new HttpContextAccessor();
            var clientContextService = new ClientContextService(httpContextAccessor);

            Assert.Null(clientContextService.IpAddress);
        }

        [Fact]
        public void IpAddress_WhenRemoteIpAddressExists_ReturnsIpAddress()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = httpContext,
            };

            var clientContextService = new ClientContextService(httpContextAccessor);

            Assert.Equal("127.0.0.1", clientContextService.IpAddress);
        }
    }
}
