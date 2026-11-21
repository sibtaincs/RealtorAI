using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RealEstateAI.Api.Tests;

[Collection("API")]
public sealed class EndpointTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", body.RootElement.GetProperty("status").GetString());
        Assert.True(body.RootElement.GetProperty("metrics").GetProperty("total").GetInt64() >= 1);
        Assert.True(body.RootElement.GetProperty("metrics").GetProperty("byStatusClass")
            .TryGetProperty("2xx", out _));
    }

    [Fact]
    public async Task Correlation_id_is_generated_and_echoed()
    {
        var generated = await _client.GetAsync("/api/health");
        Assert.True(generated.Headers.Contains("X-Correlation-Id"));
        Assert.False(string.IsNullOrWhiteSpace(
            generated.Headers.GetValues("X-Correlation-Id").Single()));

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("X-Correlation-Id", "m6-test-correlation");
        var echoed = await _client.SendAsync(request);

        Assert.Equal("m6-test-correlation",
            echoed.Headers.GetValues("X-Correlation-Id").Single());
    }

    [Fact]
    public async Task Root_describes_the_service()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("RealEstateAI API", body.RootElement.GetProperty("service").GetString());
    }

    [Fact]
    public async Task Properties_are_paginated()
    {
        var response = await _client.GetAsync("/api/properties?pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal(2, root.GetProperty("items").GetArrayLength());
        Assert.True(root.GetProperty("total").GetInt32() >= 14);
        Assert.True(root.GetProperty("items")[0].GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task Properties_filter_by_city()
    {
        var response = await _client.GetAsync("/api/properties?city=Lahore&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);
        Assert.All(items, p => Assert.Equal("Lahore", p.GetProperty("city").GetString()));
    }

    [Fact]
    public async Task Properties_sort_by_price()
    {
        var response = await _client.GetAsync("/api/properties?sort=price-asc&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var prices = body.RootElement.GetProperty("items").EnumerateArray()
            .Select(p => p.GetProperty("price").GetDecimal())
            .ToList();

        Assert.NotEmpty(prices);
        Assert.Equal(prices.OrderBy(p => p), prices);
    }

    [Fact]
    public async Task Properties_filter_by_price_and_area()
    {
        var response = await _client.GetAsync(
            "/api/properties?minPrice=50000&maxPrice=100000000&minMarla=5&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.GetProperty("items").EnumerateArray().ToList();

        Assert.NotEmpty(items);
        Assert.All(items, item =>
        {
            Assert.InRange(item.GetProperty("price").GetDecimal(), 50000m, 100000000m);
            Assert.True(item.GetProperty("areaMarla").GetDecimal() >= 5m);
        });
    }

    [Fact]
    public async Task Unknown_property_returns_404()
    {
        var response = await _client.GetAsync("/api/properties/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_log_in_and_read_own_profile()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@realestateai.pk", password = "Admin@123" });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var auth = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = auth.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Contains("Admin", auth.RootElement.GetProperty("roles").EnumerateArray()
            .Select(r => r.GetString()));

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("Authorization", $"Bearer {token}");
        var me = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        using var profile = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.Equal("admin@realestateai.pk", profile.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Invalid_login_is_unauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@realestateai.pk", password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_requires_a_token()
    {
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Allowed_frontend_origin_is_reflected_in_cors_headers()
    {
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");

        var response = await client.GetAsync("/api/health");

        Assert.Equal("http://localhost:4200",
            response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("X-Correlation-Id",
            response.Headers.GetValues("Access-Control-Expose-Headers").Single()
                .Split(',', StringSplitOptions.TrimEntries));
    }
}
