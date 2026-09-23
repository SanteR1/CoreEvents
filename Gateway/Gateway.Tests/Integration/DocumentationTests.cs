using System.Net;
using Gateway.Tests.Fixtures;

namespace Gateway.Tests.Integration;

public class DocumentationTests
{
    [Fact]
    public async Task DevelopmentEnvironment_SwaggerAndScalarEndpoints_AreAvailable()
    {
        // Arrange
        using var factory = new GatewayWebApplicationFactory(environment: "Development");
        using var client = factory.CreateClient();

        // Act
        var swaggerResponse = await client.GetAsync("/swagger/index.html", TestContext.Current.CancellationToken);
        var docsResponse = await client.GetAsync("/docs", TestContext.Current.CancellationToken);

        // Assert
        swaggerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var swaggerContent = await swaggerResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        swaggerContent.Should().Contain("swagger");

        docsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var docsContent = await docsResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        docsContent.Should().Contain("scalar");
        docsContent.Should().Contain("\"title\":\"Users API v1\"");
        docsContent.Should().Contain("\"url\":\"../openapi/users/v1.json\"");
    }

    [Fact]
    public async Task ProductionEnvironment_DocumentationEndpoints_AreDisabled()
    {
        // Arrange
        using var factory = new GatewayWebApplicationFactory(environment: "Production");
        using var client = factory.CreateClient();

        // Act
        var swaggerResponse = await client.GetAsync("/swagger/index.html", TestContext.Current.CancellationToken);
        var docsResponse = await client.GetAsync("/docs", TestContext.Current.CancellationToken);

        // Assert
        swaggerResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        docsResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
