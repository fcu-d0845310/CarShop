using System.Net;

namespace CarShop.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class ApiStartupTests(CarShopApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task OpenApiDocument_IsAvailable()
    {
        var response = await _client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"openapi\"", body);
    }

    [Fact]
    public async Task SwaggerUi_IsAvailable()
    {
        var response = await _client.GetAsync("/swagger/index.html", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnknownRoute_ReturnsProblemDetails()
    {
        var response = await _client.GetAsync("/api/not-exists", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
