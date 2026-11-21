using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public static class ContentKinds
{
    public static readonly string[] All =
        ["project", "agency", "society", "land-record", "article", "area-guide", "area-trend", "price-index"];

    public static bool IsValid(string? kind) =>
        !string.IsNullOrWhiteSpace(kind) && All.Contains(kind, StringComparer.OrdinalIgnoreCase);
}

public interface IContentService
{
    Task<IReadOnlyList<ContentItemDto>> ListAsync(string? kind);
    Task<ContentItemDto?> GetAsync(int id);
    Task<ContentItemDto> CreateAsync(ContentUpsertDto input);
    Task<ContentItemDto?> UpdateAsync(int id, ContentUpsertDto input);
    Task<bool> DeleteAsync(int id);
}

public class ContentService : IContentService
{
    private readonly AppDbContext _db;

    public ContentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ContentItemDto>> ListAsync(string? kind)
    {
        var query = _db.ContentItems.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(kind))
            query = query.Where(c => c.Kind == kind.ToLowerInvariant());

        return await query.OrderBy(c => c.Kind).ThenBy(c => c.Id)
            .Select(c => new ContentItemDto(c.Id, c.Kind, c.PayloadJson, c.UpdatedAt))
            .ToListAsync();
    }

    public async Task<ContentItemDto?> GetAsync(int id) =>
        await _db.ContentItems.AsNoTracking().Where(c => c.Id == id)
            .Select(c => new ContentItemDto(c.Id, c.Kind, c.PayloadJson, c.UpdatedAt))
            .FirstOrDefaultAsync();

    public async Task<ContentItemDto> CreateAsync(ContentUpsertDto input)
    {
        var item = new ContentItem
        {
            Kind = input.Kind.Trim().ToLowerInvariant(),
            PayloadJson = input.PayloadJson.Trim()
        };
        _db.ContentItems.Add(item);
        await _db.SaveChangesAsync();
        return Dto(item);
    }

    public async Task<ContentItemDto?> UpdateAsync(int id, ContentUpsertDto input)
    {
        var item = await _db.ContentItems.FindAsync(id);
        if (item is null) return null;

        item.Kind = input.Kind.Trim().ToLowerInvariant();
        item.PayloadJson = input.PayloadJson.Trim();
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Dto(item);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.ContentItems.FindAsync(id);
        if (item is null) return false;

        _db.ContentItems.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    private static ContentItemDto Dto(ContentItem c) =>
        new(c.Id, c.Kind, c.PayloadJson, c.UpdatedAt);
}
