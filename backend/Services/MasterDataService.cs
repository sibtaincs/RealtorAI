using Microsoft.EntityFrameworkCore;
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

    public MasterDataService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MasterDataDto>> ListAsync(string? kind)
    {
        var query = _db.MasterData.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(kind))
            query = query.Where(m => m.Kind == kind.ToLowerInvariant());

        return await query.OrderBy(m => m.Kind).ThenBy(m => m.SortOrder).ThenBy(m => m.Id)
            .Select(m => new MasterDataDto(m.Id, m.Kind, m.Value, m.Label, m.SortOrder, m.IsActive))
            .ToListAsync();
    }

    public async Task<MasterDataDto?> GetAsync(int id) =>
        await _db.MasterData.AsNoTracking().Where(m => m.Id == id)
            .Select(m => new MasterDataDto(m.Id, m.Kind, m.Value, m.Label, m.SortOrder, m.IsActive))
            .FirstOrDefaultAsync();

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
        return Dto(item);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.MasterData.FindAsync(id);
        if (item is null) return false;

        _db.MasterData.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    private static MasterDataDto Dto(MasterData m) =>
        new(m.Id, m.Kind, m.Value, m.Label, m.SortOrder, m.IsActive);
}
