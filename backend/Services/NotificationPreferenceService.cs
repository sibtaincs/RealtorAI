using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface INotificationPreferenceService
{
    Task<NotificationPreferenceDto> GetAsync(string userId);
    Task<NotificationPreferenceDto> UpdateAsync(string userId, UpdateNotificationPreferenceDto input);
}

public class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly AppDbContext _db;

    public NotificationPreferenceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<NotificationPreferenceDto> GetAsync(string userId)
    {
        var prefs = await _db.NotificationPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);
        return ToDto(prefs);
    }

    public async Task<NotificationPreferenceDto> UpdateAsync(string userId, UpdateNotificationPreferenceDto input)
    {
        var prefs = await _db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prefs is null)
        {
            prefs = new NotificationPreference { UserId = userId };
            _db.NotificationPreferences.Add(prefs);
        }

        prefs.InAppEnabled = input.InAppEnabled;
        prefs.EmailEnabled = input.EmailEnabled;
        prefs.WhatsAppEnabled = input.WhatsAppEnabled;
        await _db.SaveChangesAsync();
        return ToDto(prefs);
    }

    private static NotificationPreferenceDto ToDto(NotificationPreference? prefs)
        => prefs is null
            ? new NotificationPreferenceDto(true, false, false)
            : new NotificationPreferenceDto(prefs.InAppEnabled, prefs.EmailEnabled, prefs.WhatsAppEnabled);
}
