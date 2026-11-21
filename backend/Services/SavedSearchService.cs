using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface ISavedSearchService
{
    Task<List<SavedSearchDto>> GetAsync(string userId);
    Task<SavedSearchDto?> CreateAsync(string userId, CreateSavedSearchDto input);
    Task<SavedSearchDto?> UpdateAsync(int id, string userId, UpdateSavedSearchDto input);
    Task<bool> DeleteAsync(int id, string userId);
    Task<int> RunAsync(int id);
    Task<List<int>> GetAllEnabledIdsAsync();
}

public class SavedSearchService : ISavedSearchService
{
    private readonly AppDbContext _db;
    private readonly IPropertyService _properties;
    private readonly IAlertDeliveryService _delivery;

    public SavedSearchService(AppDbContext db, IPropertyService properties, IAlertDeliveryService delivery)
    {
        _db = db;
        _properties = properties;
        _delivery = delivery;
    }

    public async Task<List<SavedSearchDto>> GetAsync(string userId)
    {
        return await _db.SavedSearches
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SavedSearchDto(
                s.Id, s.Name, s.QueryJson, s.AlertEnabled, s.CreatedAt, s.LastAlertAt))
            .ToListAsync();
    }

    public async Task<SavedSearchDto?> CreateAsync(string userId, CreateSavedSearchDto input)
    {
        var search = new SavedSearch
        {
            UserId = userId,
            Name = input.Name,
            QueryJson = JsonSerializer.Serialize(input.Query),
            AlertEnabled = input.AlertEnabled,
            LastAlertAt = DateTime.UtcNow
        };

        _db.SavedSearches.Add(search);
        await _db.SaveChangesAsync();
        return ToDto(search);
    }

    public async Task<SavedSearchDto?> UpdateAsync(int id, string userId, UpdateSavedSearchDto input)
    {
        var search = await _db.SavedSearches
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (search is null) return null;

        search.Name = input.Name;
        search.AlertEnabled = input.AlertEnabled;
        await _db.SaveChangesAsync();
        return ToDto(search);
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var search = await _db.SavedSearches
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (search is null) return false;

        _db.SavedSearches.Remove(search);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Re-runs a saved search, creates a notification for new matches and returns the match count.
    /// </summary>
    public async Task<int> RunAsync(int id)
    {
        var search = await _db.SavedSearches.FindAsync(id);
        if (search is null) return 0;

        PropertyQueryDto query;
        try
        {
            using var doc = JsonDocument.Parse(search.QueryJson);
            var root = doc.RootElement;

            string? Get(string key) =>
                root.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null
                    ? value.ToString()
                    : null;

            decimal? GetDecimal(string key) =>
                decimal.TryParse(Get(key), out var number) ? number : null;

            int? GetInt(string key) =>
                int.TryParse(Get(key), out var number) ? number : null;

            var type = Get("type");
            var listingType = Get("listingType");

            query = new PropertyQueryDto(
                Get("search"), Get("city"), Get("locality"), type, listingType, null,
                GetDecimal("minPrice"), GetDecimal("maxPrice"),
                GetInt("minBeds"), GetInt("minMarla"), 1, 50);
        }
        catch
        {
            query = new PropertyQueryDto(null, null, null, null, null, null, null, null, null, null, 1, 50);
        }

        var result = await _properties.GetAsync(new PropertyQueryDto(
            query.Search, query.City, query.Locality, query.Type, query.ListingType, null,
            query.MinPrice, query.MaxPrice, query.MinBeds, query.MinMarla, 1, 50));

        var since = search.LastAlertAt ?? DateTime.UtcNow.AddDays(-7);
        var newItems = result.Items.Where(p => p.CreatedAt > since).ToList();

        if (newItems.Count > 0)
            await _delivery.DeliverAsync(search, newItems);

        search.LastAlertAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return newItems.Count;
    }

    public async Task<List<int>> GetAllEnabledIdsAsync()
        => await _db.SavedSearches
            .AsNoTracking()
            .Where(s => s.AlertEnabled)
            .Select(s => s.Id)
            .ToListAsync();

    private static SavedSearchDto ToDto(SavedSearch s) => new(
        s.Id, s.Name, s.QueryJson, s.AlertEnabled, s.CreatedAt, s.LastAlertAt);
}

