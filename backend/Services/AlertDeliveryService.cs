using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface IAlertDeliveryService
{
    Task DeliverAsync(SavedSearch search, IReadOnlyList<PropertyDto> newItems);
}

/// <summary>
/// Fans a saved-search match out to the channels the user opted into.
/// In-app stays the default; email and WhatsApp are opt-in and offline-safe.
/// </summary>
public class AlertDeliveryService : IAlertDeliveryService
{
    private readonly AppDbContext _db;
    private readonly INotificationPreferenceService _preferences;
    private readonly IEmailSender _email;
    private readonly IWhatsAppService _whatsapp;
    private readonly IConfiguration _config;
    private readonly ILogger<AlertDeliveryService> _logger;

    public AlertDeliveryService(
        AppDbContext db,
        INotificationPreferenceService preferences,
        IEmailSender email,
        IWhatsAppService whatsapp,
        IConfiguration config,
        ILogger<AlertDeliveryService> logger)
    {
        _db = db;
        _preferences = preferences;
        _email = email;
        _whatsapp = whatsapp;
        _config = config;
        _logger = logger;
    }

    public async Task DeliverAsync(SavedSearch search, IReadOnlyList<PropertyDto> newItems)
    {
        if (newItems.Count == 0) return;

        var prefs = await _preferences.GetAsync(search.UserId);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == search.UserId);
        var ids = newItems.Select(i => i.Id).Distinct().ToList();
        var existing = await _db.NotificationDeliveries.AsNoTracking()
            .Where(d => d.SavedSearchId == search.Id && ids.Contains(d.PropertyListingId))
            .Select(d => new { d.PropertyListingId, d.Channel })
            .ToListAsync();
        var sent = existing.GroupBy(d => d.Channel)
            .ToDictionary(g => g.Key, g => g.Select(d => d.PropertyListingId).ToHashSet());

        var fresh = newItems.Where(i => !WasSent(sent, "InApp", i.Id)).ToList();
        var email = prefs.EmailEnabled && !string.IsNullOrWhiteSpace(user?.Email)
            ? newItems.Where(i => !WasSent(sent, "Email", i.Id)).ToList()
            : new List<PropertyDto>();
        var whatsapp = prefs.WhatsAppEnabled && !string.IsNullOrWhiteSpace(user?.PhoneNumber)
            ? newItems.Where(i => !WasSent(sent, "WhatsApp", i.Id)).ToList()
            : new List<PropertyDto>();

        if (fresh.Count == 0 && email.Count == 0 && whatsapp.Count == 0) return;

        var title = $"New matches for \"{search.Name}\"";

        if (prefs.InAppEnabled && fresh.Count > 0)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = search.UserId,
                Title = title,
                Message = $"{fresh.Count} new propert{(fresh.Count == 1 ? "y" : "ies")} match your saved search. Tap to view them.",
                Link = $"/?saved={search.Id}",
                IsRead = false
            });
            await RecordAsync(search, fresh, "InApp");
        }

        if (email.Count > 0)
        {
            await _email.SendAsync(user!.Email!, title, BuildMessage(search, email));
            await RecordAsync(search, email, "Email");
        }

        if (whatsapp.Count > 0)
        {
            var message = BuildMessage(search, whatsapp);
            if (await _whatsapp.SendTextAsync(user!.PhoneNumber!, message))
            {
                await RecordAsync(search, whatsapp, "WhatsApp");
            }
            else
            {
                // No Meta credentials: keep the wa.me link in-app instead of pretending to send.
                _db.Notifications.Add(new Notification
                {
                    UserId = search.UserId,
                    Title = "WhatsApp alert ready",
                    Message = "Meta credentials are not configured — tap to open WhatsApp with the alert text.",
                    Link = _whatsapp.BuildLink(user.PhoneNumber!, message),
                    IsRead = false
                });
                _logger.LogWarning("WhatsApp alert fell back to wa.me for user {UserId}.", search.UserId);
                await _db.SaveChangesAsync();
            }
        }
    }

    private static bool WasSent(Dictionary<string, HashSet<int>> sent, string channel, int id)
        => sent.TryGetValue(channel, out var ids) && ids.Contains(id);

    private async Task RecordAsync(SavedSearch search, IReadOnlyList<PropertyDto> items, string channel)
    {
        foreach (var id in items.Select(i => i.Id).Distinct())
        {
            _db.NotificationDeliveries.Add(new NotificationDelivery
            {
                UserId = search.UserId,
                SavedSearchId = search.Id,
                PropertyListingId = id,
                Channel = channel,
                SentAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
    }

    private string BuildMessage(SavedSearch search, IReadOnlyList<PropertyDto> items)
    {
        var lines = items.Take(3).Select(p =>
            $"• {p.Title} — {AiService.FormatPkr(p.Price, p.ListingType)}");
        var extra = items.Count > 3 ? $"\nAur {items.Count - 3} properties hain." : "";
        var baseUrl = _config["Alerts:FrontendUrl"] ?? "http://localhost:4200";

        return $"Assalam-o-Alaikum! Aapki saved search \"{search.Name}\" ke {items.Count} naye matches hain:\n\n" +
               string.Join("\n", lines) + extra + $"\n\n{baseUrl}/?saved={search.Id}";
    }
}
