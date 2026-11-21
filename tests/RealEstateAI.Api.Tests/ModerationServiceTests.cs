using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;
using RealEstateAI.Api.Services;
using Xunit;

namespace RealEstateAI.Api.Tests;

/// <summary>
/// Write paths run against in-memory SQLite so the shared development DB stays read-only.
/// </summary>
public sealed class ModerationServiceTests
{
    [Fact]
    public async Task New_reviews_are_pending_until_approved()
    {
        await using var db = await CreateDbAsync();
        Seed(db);
        var reviews = new ReviewService(db);
        var moderation = new ModerationService(db);

        var created = await reviews.CreateAsync("u1", new CreateReviewDto(1, 5, "Great agent"));
        Assert.Equal("Pending", created.ModerationStatus);
        Assert.Empty(await reviews.GetApprovedAsync(1, null));

        Assert.True(await moderation.ModerateReviewAsync(created.Id, true));
        var approved = Assert.Single(await reviews.GetApprovedAsync(1, null));
        Assert.Equal("Great agent", approved.Comment);
        Assert.Equal(5, await db.AgentProfiles.Where(p => p.UserId == "a1")
            .Select(p => p.RatingAvg).SingleAsync());
    }

    [Fact]
    public async Task Reports_leave_the_open_queue_when_resolved()
    {
        await using var db = await CreateDbAsync();
        Seed(db);
        var reports = new ReportService(db);
        var moderation = new ModerationService(db);

        var created = await reports.CreateAsync("u1", new CreateReportDto(1, "Fake photos"));
        Assert.Equal("Open", created.Status);
        Assert.Single(await moderation.GetOpenReportsAsync());

        Assert.True(await moderation.ResolveReportAsync(created.Id));
        Assert.Empty(await moderation.GetOpenReportsAsync());
    }

    private static async Task<AppDbContext> CreateDbAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static void Seed(AppDbContext db)
    {
        db.Users.AddRange(
            new ApplicationUser { Id = "u1", UserName = "buyer@test.pk", Email = "buyer@test.pk", FullName = "Buyer" },
            new ApplicationUser { Id = "a1", UserName = "agent@test.pk", Email = "agent@test.pk", FullName = "Agent" });
        db.Properties.Add(new PropertyListing
        {
            Id = 1, Title = "Test", Description = "Test", Price = 1m,
            Type = PropertyType.House, ListingType = ListingType.Sale, Status = ListingStatus.Active,
            City = "Lahore", Locality = "DHA", Address = "Test", Latitude = 0, Longitude = 0,
            ImageUrlsJson = "[]", AgentId = "a1"
        });
        db.AgentProfiles.Add(new AgentProfile { UserId = "a1", Bio = "Test agent" });
        db.SaveChanges();
    }
}
