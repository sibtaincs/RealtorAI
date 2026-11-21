using System.Net;
using System.Text.Json;
using Xunit;

namespace RealEstateAI.Api.Tests;

[Collection("API")]
public sealed class PropertyMapTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task Properties_can_be_filtered_by_bounding_box()
    {
        var response = await _client.GetAsync(
            "/api/properties?minLat=31.40&maxLat=31.50&minLng=74.30&maxLng=74.40&pageSize=48");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.GetProperty("items").EnumerateArray().ToList();

        Assert.NotEmpty(items);
        Assert.All(items, item =>
        {
            Assert.InRange(item.GetProperty("latitude").GetDouble(), 31.40, 31.50);
            Assert.InRange(item.GetProperty("longitude").GetDouble(), 74.30, 74.40);
        });
    }

    [Fact]
    public async Task Radius_query_uses_a_lat_lng_box_approximation()
    {
        const double centerLat = 31.45;
        const double centerLng = 74.38;
        var latDelta = 5.0 / 111.0;
        var lngDelta = 5.0 / (111.0 * Math.Cos(centerLat * Math.PI / 180.0));

        var response = await _client.GetAsync(
            $"/api/properties?centerLat={centerLat}&centerLng={centerLng}&radiusKm=5&pageSize=48");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.GetProperty("items").EnumerateArray().ToList();

        Assert.NotEmpty(items);
        Assert.All(items, item =>
        {
            Assert.InRange(item.GetProperty("latitude").GetDouble(),
                centerLat - latDelta, centerLat + latDelta);
            Assert.InRange(item.GetProperty("longitude").GetDouble(),
                centerLng - lngDelta, centerLng + lngDelta);
        });
    }
}
