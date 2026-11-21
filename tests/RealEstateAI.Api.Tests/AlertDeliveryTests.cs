using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;
using RealEstateAI.Api.Services;
using Xunit;

namespace RealEstateAI.Api.Tests;

/// <summary>
/// Runs the delivery service against an in-memory SQLite DB with fake senders —
/// no external calls and no writes to the shared development database.
/// </summary>
public sealed class AlertDeliveryTests
{
    [Fact]
    public async Task Delivery_fans_out_to_enabled_channels_and_dedupes_repeat_runs()
    {
        await using var db = await CreateDbAsync();
        var search = await SeedAsync(db, inApp: true, email: true, whatsapp: false);
        var email = new FakeEmailSender();
        var whatsapp = new FakeWhatsAppService();
        var service = CreateService(db, email, whatsapp);

        var items = new[] { Property(1), Property(2) };
        await service.DeliverAsync(search, items);
        await service.DeliverAsync(search, items);

        Assert.Single(await db.Notifications.AsNoTracking().ToListAsync());
        Assert.Single(email.Sent);
        Assert.Empty(whatsapp.Messages);

        var deliveries = await db.NotificationDeliveries.AsNoTracking().ToListAsync();
        Assert.Equal(4, deliveries.Count);
        Assert.Equal(2, deliveries.Count(d => d.Channel == "InApp"));
        Assert.Equal(2, deliveries.Count(d => d.Channel == "Email"));
    }

    [Fact]
    public async Task Delivery_without_meta_credentials_keeps_a_wa_me_link_in_app()
    {
        await using var db = await CreateDbAsync();
        var search = await SeedAsync(db, inApp: false, email: false, whatsapp: true);
        var whatsapp = new FakeWhatsAppService { Sent = false };
        var service = CreateService(db, new FakeEmailSender(), whatsapp);

        await service.DeliverAsync(search, new[] { Property(1) });

        var notification = Assert.Single(await db.Notifications.AsNoTracking().ToListAsync());
        Assert.StartsWith("https://wa.me/", notification.Link);
        Assert.Empty(await db.NotificationDeliveries.AsNoTracking().ToListAsync());
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

    private static async Task<SavedSearch> SeedAsync(AppDbContext db, bool inApp, bool email, bool whatsapp)
    {
        db.Users.Add(new ApplicationUser
        {
            Id = "u1",
            UserName = "agent@test.pk",
            Email = "agent@test.pk",
            PhoneNumber = "+923001234567"
        });
        var search = new SavedSearch { Id = 1, UserId = "u1", Name = "Lahore houses", QueryJson = "{}" };
        db.SavedSearches.Add(search);
        db.NotificationPreferences.Add(new NotificationPreference
        {
            UserId = "u1",
            InAppEnabled = inApp,
            EmailEnabled = email,
            WhatsAppEnabled = whatsapp
        });
        await db.SaveChangesAsync();
        return search;
    }

    private static AlertDeliveryService CreateService(
        AppDbContext db, IEmailSender email, IWhatsAppService whatsapp)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Alerts:FrontendUrl"] = "http://localhost:4200"
            })
            .Build();
        return new AlertDeliveryService(
            db, new NotificationPreferenceService(db), email, whatsapp, config,
            NullLogger<AlertDeliveryService>.Instance);
    }

    private static PropertyDto Property(int id) => new(
        id, $"House {id}", "Test listing", 5_000_000m, "House", "Sale", "Active",
        "Lahore", "DHA", "Test address", 0, 0, 5, 4, 10, null,
        Array.Empty<string>(), false, DateTime.UtcNow);

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<string> Sent { get; } = new();

        public Task SendAsync(string to, string subject, string body)
        {
            Sent.Add(to);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWhatsAppService : IWhatsAppService
    {
        public List<string> Messages { get; } = new();
        public bool Sent { get; set; } = true;

        public Task<WhatsAppResultDto> SendLeadFollowUpAsync(Lead lead, PropertyListing? property)
            => Task.FromResult(new WhatsAppResultDto(null, false, string.Empty));

        public Task<bool> SendTextAsync(string phone, string message)
        {
            Messages.Add(message);
            return Task.FromResult(Sent);
        }

        public string BuildLink(string phone, string message)
            => $"https://wa.me/{phone}?text=test";
    }
}
