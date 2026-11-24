using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public static class MasterDataKinds
{
    public static readonly string[] All =
    [
        "city", "property-type", "listing-type", "currency", "area-unit",
        "bedroom", "sort-option", "project-status", "agency-specialty",
        "article-category", "land-record-status", "lead-status",
        "listing-status", "report-status", "moderation-status", "price-advisor-days"
    ];

    // Workflow codes and display units are consumed by fixed backend logic; admins can relabel them but not add new ones.
    public static readonly string[] FixedValueKinds =
    [
        "currency", "area-unit", "lead-status", "listing-status",
        "report-status", "moderation-status"
    ];

    public static bool IsValid(string? kind) =>
        !string.IsNullOrWhiteSpace(kind) && All.Contains(kind, StringComparer.OrdinalIgnoreCase);

    public static bool HasFixedValues(string? kind) =>
        !string.IsNullOrWhiteSpace(kind) && FixedValueKinds.Contains(kind, StringComparer.OrdinalIgnoreCase);
}

public interface IMasterDataService
{
    Task<IReadOnlyList<MasterDataDto>> ListAsync(string? kind);
    Task<MasterDataDto?> GetAsync(int id);
    Task<MasterDataDto> CreateAsync(MasterDataUpsertDto input);
    Task<MasterDataDto?> UpdateAsync(int id, MasterDataUpsertDto input);
    Task<bool> DeleteAsync(int id);
}

public class MasterDataService : IMasterDataService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public MasterDataService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<MasterDataDto>> ListAsync(string? kind)
    {
        var listKey = $"master:list:{kind ?? "all"}";
        if (_cache.TryGetValue<List<MasterDataDto>>(listKey, out var cached) && cached is not null) return cached;

        var query = _db.MasterData.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(kind))
            query = query.Where(m => m.Kind == kind.ToLowerInvariant());

        var items = await query.OrderBy(m => m.Kind).ThenBy(m => m.SortOrder).ThenBy(m => m.Id)
            .Select(m => new MasterDataDto(m.Id, m.Kind, m.Value, m.Label, m.SortOrder, m.IsActive))
            .ToListAsync();
        _cache.Set(listKey, items, CacheTtl);
        return items;
    }

    public async Task<MasterDataDto?> GetAsync(int id)
    {
        var getKey = $"master:get:{id}";
        if (_cache.TryGetValue<MasterDataDto>(getKey, out var cached) && cached is not null) return cached;

        var item = await _db.MasterData.AsNoTracking().Where(m => m.Id == id)
            .Select(m => new MasterDataDto(m.Id, m.Kind, m.Value, m.Label, m.SortOrder, m.IsActive))
            .FirstOrDefaultAsync();
        if (item is null) return null;
        _cache.Set(getKey, item, CacheTtl);
        return item;
    }

    public async Task<MasterDataDto> CreateAsync(MasterDataUpsertDto input)
    {
        var item = new MasterData
        {
            Kind = input.Kind.Trim().ToLowerInvariant(),
            Value = input.Value.Trim(),
            Label = string.IsNullOrWhiteSpace(input.Label) ? input.Value.Trim() : input.Label.Trim(),
            SortOrder = input.SortOrder,
            IsActive = input.IsActive
        };
        _db.MasterData.Add(item);
        await _db.SaveChangesAsync();
        _cache.Remove($"master:list:{input.Kind.Trim().ToLowerInvariant()}");
        _cache.Remove($"master:list:all");
        return Dto(item);
    }

    public async Task<MasterDataDto?> UpdateAsync(int id, MasterDataUpsertDto input)
    {
        var item = await _db.MasterData.FindAsync(id);
        if (item is null) return null;

        item.Kind = input.Kind.Trim().ToLowerInvariant();
        item.Value = input.Value.Trim();
        item.Label = string.IsNullOrWhiteSpace(input.Label) ? input.Value.Trim() : input.Label.Trim();
        item.SortOrder = input.SortOrder;
        item.IsActive = input.IsActive;
        await _db.SaveChangesAsync();
        _cache.Remove($"master:list:{item.Kind}");
        _cache.Remove($"master:list:all");
        _cache.Remove($"master:get:{item.Id}");
        return Dto(item);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.MasterData.FindAsync(id);
        if (item is null) return false;

        _db.MasterData.Remove(item);
        await _db.SaveChangesAsync();
        _cache.Remove($"master:get:{item.Id}");
        _cache.Remove($"master:list:{item.Kind}");
        _cache.Remove($"master:list:all");
        return true;
    }

    private static MasterDataDto Dto(MasterData m) =>
        new(m.Id, m.Kind, m.Value, m.Label, m.SortOrder, m.IsActive);
}
