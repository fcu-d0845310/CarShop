using System.Net;

namespace CarShop.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class StaticFileTests(CarShopApiFactory factory)
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SeedImage_IsServedFromWwwroot()
    {
        var response = await _client.GetAsync("/images/seed/car-1-1.png", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        Assert.Equal(PngSignature, bytes.Take(PngSignature.Length));
    }
}
