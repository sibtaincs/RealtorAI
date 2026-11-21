using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RealEstateAI.Api.Data;
using Xunit;

namespace RealEstateAI.Api.Tests;

[Collection("API")]
public sealed class TrustTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task Public_reviews_only_expose_approved_items()
    {
        var response = await _client.GetAsync("/api/reviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.EnumerateArray().ToList();

        Assert.NotEmpty(items);
        Assert.All(items, item =>
        {
            Assert.Equal("Approved", item.GetProperty("moderationStatus").GetString());
            Assert.InRange(item.GetProperty("rating").GetInt32(), 1, 5);
        });
    }

    [Fact]
    public async Task Moderation_queues_are_admin_only()
    {
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.GetAsync("/api/moderation/reviews")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.GetAsync("/api/moderation/reports")).StatusCode);

        using var reviews = await GetAuthenticatedAsync("/api/moderation/reviews");
        using var reports = await GetAuthenticatedAsync("/api/moderation/reports");

        Assert.Equal(HttpStatusCode.OK, reviews.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reports.StatusCode);
        using var reviewBody = JsonDocument.Parse(await reviews.Content.ReadAsStringAsync());
        using var reportBody = JsonDocument.Parse(await reports.Content.ReadAsStringAsync());

        Assert.Equal(JsonValueKind.Array, reviewBody.RootElement.ValueKind);
        Assert.Equal(JsonValueKind.Array, reportBody.RootElement.ValueKind);
    }

    [Fact]
    public async Task Agent_profile_and_properties_are_publicly_readable()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var agentId = await db.Users.Where(u => u.Email == "agent@realestateai.pk")
            .Select(u => u.Id).FirstOrDefaultAsync();
        Assert.False(string.IsNullOrEmpty(agentId));

        var profile = await _client.GetAsync($"/api/agents/{agentId}");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        using var profileBody = JsonDocument.Parse(await profile.Content.ReadAsStringAsync());
        Assert.True(profileBody.RootElement.GetProperty("verified").GetBoolean());
        Assert.True(profileBody.RootElement.GetProperty("ratingCount").GetInt32() > 0);

        var listings = await _client.GetAsync($"/api/properties?agentId={agentId}");
        Assert.Equal(HttpStatusCode.OK, listings.StatusCode);
        using var listingBody = JsonDocument.Parse(await listings.Content.ReadAsStringAsync());
        Assert.True(listingBody.RootElement.GetProperty("total").GetInt32() > 0);
        Assert.All(listingBody.RootElement.GetProperty("items").EnumerateArray(),
            item => Assert.Equal(agentId, item.GetProperty("agentId").GetString()));
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
