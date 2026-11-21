using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RealEstateAI.Api.Tests;

[Collection("API")]
public sealed class DashboardTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task Dashboard_requires_a_token()
    {
        var response = await _client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_read_dashboard_summary()
    {
        var response = await GetAuthenticatedAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;

        Assert.True(root.GetProperty("totalLeads").GetInt32() >= 0);
        Assert.True(root.GetProperty("newLeads7Days").GetInt32() >= 0);
        Assert.Equal(root.GetProperty("totalLeads").GetInt32(),
            root.GetProperty("hotLeads").GetInt32() +
            root.GetProperty("warmLeads").GetInt32() +
            root.GetProperty("coldLeads").GetInt32());
        Assert.True(root.GetProperty("activeListings").GetInt32() >= 0);
        Assert.True(root.GetProperty("averageFirstResponseHours").GetDouble() >= 0);
    }

    [Fact]
    public async Task Dashboard_leads_are_sorted_by_score()
    {
        var response = await GetAuthenticatedAsync("/api/dashboard/leads?pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var scores = body.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("score").GetInt32())
            .ToList();

        Assert.Equal(scores.OrderByDescending(s => s), scores);
        Assert.True(body.RootElement.GetProperty("pageSize").GetInt32() == 50);
    }

    [Fact]
    public async Task Dashboard_leads_can_filter_by_temperature()
    {
        var response = await GetAuthenticatedAsync("/api/dashboard/leads?temperature=Hot&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.GetProperty("items").EnumerateArray().ToList();

        Assert.All(items, item => Assert.Equal("Hot", item.GetProperty("scoreLabel").GetString()));
    }

    [Fact]
    public async Task Dashboard_listings_include_verification_counts()
    {
        var response = await GetAuthenticatedAsync("/api/dashboard/listings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.EnumerateArray().ToList();

        Assert.NotEmpty(items);
        Assert.All(items, item =>
        {
            Assert.True(item.GetProperty("verifiedDocuments").GetInt32() <=
                        item.GetProperty("totalDocuments").GetInt32());
            Assert.True(item.GetProperty("leadCount").GetInt32() >= 0);
            Assert.True(item.GetProperty("daysListed").GetInt32() >= 0);
            _ = item.GetProperty("isTrusted").GetBoolean();
        });
    }

    private async Task<HttpResponseMessage> GetAuthenticatedAsync(string url)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@realestateai.pk", password = "Admin@123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using var auth = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = auth.RootElement.GetProperty("token").GetString();

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {token}");
        return await _client.SendAsync(request);
    }
}
