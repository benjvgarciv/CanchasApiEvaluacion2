using System.Net;
using Xunit;

namespace CanchasApi.Tests
{
    public class HealthTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public HealthTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Health_DeberiaResponder200Y_ConexionOk()
        {
            var response = await _client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("connected", body);
        }
    }
}
