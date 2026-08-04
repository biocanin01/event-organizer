using System.Net;

namespace EventOrganizer.Tests.Api
{
    public sealed class CorsEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CorsEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Preflight_FromConfiguredFrontendOrigin_ReturnsCorsHeaders()
        {
            var client = _factory.CreateClient();
            using var request = new HttpRequestMessage(
                HttpMethod.Options,
                "/api/events");
            request.Headers.Add("Origin", "http://localhost:5173");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(
                "http://localhost:5173",
                response.Headers.GetValues("Access-Control-Allow-Origin").Single());
            Assert.Equal(
                "true",
                response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        }
    }
}
