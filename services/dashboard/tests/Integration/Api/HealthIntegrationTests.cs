using System.Net;
using ArchChallenge.Dashboard.Tests.Integration.Support;
using FluentAssertions;

namespace ArchChallenge.Dashboard.Tests.Integration.Api;

public class HealthIntegrationTests(DashboardWebApplicationFactory factory) : IClassFixture<DashboardWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GET_health_liveness_ShouldReturn200()
    {
        var response = await _client.GetAsync("/health/liveness");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
