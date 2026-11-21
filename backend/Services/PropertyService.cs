using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface IPropertyService
{
    Task<PagedResultDto<PropertyDto>> GetAsync(PropertyQueryDto query);
    Task<PropertyDto?> GetByIdAsync(int id);
    Task<PropertyDto> CreateAsync(PropertyInputDto input, string? agentId);
    Task<PropertyDto?> UpdateAsync(int id, PropertyInputDto input);
    Task<bool> DeleteAsync(int id);
    Task<List<PropertyDto>> GetForAiContextAsync(int take = 24);
}

public class PropertyService : IPropertyService
{
    private readonly AppDbContext _db;

    public PropertyService(AppDbContext db)
    {
        _db = db;
    }

    public static PropertyDto ToDto(PropertyListing p, int verified = 0, int total = 0) => new(
        p.Id,
        p.Title,
        p.Description,
        p.Price,
        p.Type,
        p.ListingType,
        p.Status.ToString(),
        p.City,
        p.Locality,
        p.Address,
        p.Latitude,
        p.Longitude,
        p.Bedrooms,
        p.Bathrooms,
        p.AreaMarla,
        p.AreaSqFt,
        ParseImages(p.ImageUrlsJson),
        p.IsFeatured,
        p.CreatedAt,
        verified,
        total,
        total > 0 && verified >= Math.Max(3, (int)Math.Ceiling(total * 0.6)),
        p.AgentId);

    public async Task<PagedResultDto<PropertyDto>> GetAsync(PropertyQueryDto query)
    {
        var q = _db.Properties.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(p =>
                p.Title.Contains(s) || p.Description.Contains(s) ||
                p.Locality.Contains(s) || p.City.Contains(s) || p.Address.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(query.City))
            q = q.Where(p => p.City.Contains(query.City));

        if (!string.IsNullOrWhiteSpace(query.Locality))
            q = q.Where(p => p.Locality.Contains(query.Locality));

        if (!string.IsNullOrWhiteSpace(query.Type))
            q = q.Where(p => p.Type == query.Type);

        if (!string.IsNullOrWhiteSpace(query.ListingType))
            q = q.Where(p => p.ListingType == query.ListingType);

        if (query.MinPrice.HasValue)
            q = q.Where(p => p.Price >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            q = q.Where(p => p.Price <= query.MaxPrice.Value);

        if (query.MinBeds.HasValue)
            q = q.Where(p => p.Bedrooms >= query.MinBeds.Value);

        if (query.MinMarla.HasValue)
            q = q.Where(p => p.AreaMarla >= query.MinMarla.Value);

        if (!string.IsNullOrWhiteSpace(query.AgentId))
            q = q.Where(p => p.AgentId == query.AgentId);

        if (query.MinLat.HasValue)
            q = q.Where(p => p.Latitude >= query.MinLat.Value);
        if (query.MaxLat.HasValue)
            q = q.Where(p => p.Latitude <= query.MaxLat.Value);
        if (query.MinLng.HasValue)
            q = q.Where(p => p.Longitude >= query.MinLng.Value);
        if (query.MaxLng.HasValue)
            q = q.Where(p => p.Longitude <= query.MaxLng.Value);

        // Radius is a lat/lng box approximation so it stays translatable on SQL Server and Postgres.
        if (query.CenterLat.HasValue && query.CenterLng.HasValue &&
            query.RadiusKm.HasValue && query.RadiusKm.Value > 0)
        {
            var latDelta = query.RadiusKm.Value / 111.0;
            var lngDelta = query.RadiusKm.Value /
                (111.0 * Math.Cos(query.CenterLat.Value * Math.PI / 180.0));
            q = q.Where(p =>
                p.Latitude >= query.CenterLat.Value - latDelta &&
                p.Latitude <= query.CenterLat.Value + latDelta &&
                p.Longitude >= query.CenterLng.Value - lngDelta &&
                p.Longitude <= query.CenterLng.Value + lngDelta);
        }

        var total = await q.CountAsync();

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 48);

        var ordered = query.Sort switch
        {
            "price-asc" => q.OrderBy(p => p.Price).ThenBy(p => p.Id),
            "price-desc" => q.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            "area-desc" => q.OrderByDescending(p => p.AreaMarla ?? 0).ThenBy(p => p.Id),
            _ => q.OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.CreatedAt)
                .ThenBy(p => p.Id)
        };

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var counts = await GetVerificationCountsAsync(items.Select(i => i.Id).ToList());
        return new PagedResultDto<PropertyDto>(
            items.Select(p => ToDto(p, counts.GetValueOrDefault(p.Id).Verified, counts.GetValueOrDefault(p.Id).Total)).ToList(),
            total, page, pageSize);
    }

    public async Task<PropertyDto?> GetByIdAsync(int id)
    {
        var p = await _db.Properties.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return null;
        var counts = await GetVerificationCountsAsync(new[] { id });
        var c = counts.GetValueOrDefault(id);
        return ToDto(p, c.Verified, c.Total);
    }

    private async Task<Dictionary<int, (int Verified, int Total)>> GetVerificationCountsAsync(IReadOnlyCollection<int> ids)
    {
        if (ids.Count == 0) return new Dictionary<int, (int, int)>();

        var rows = await _db.DocumentChecklist
            .AsNoTracking()
            .Where(d => ids.Contains(d.PropertyListingId))
            .GroupBy(d => d.PropertyListingId)
            .Select(g => new
            {
                Id = g.Key,
                Verified = g.Count(d => d.IsVerified),
                Total = g.Count()
            })
            .ToListAsync();

        return rows.ToDictionary(r => r.Id, r => (r.Verified, r.Total));
    }

    public async Task<PropertyDto> CreateAsync(PropertyInputDto input, string? agentId)
    {
        var entity = new PropertyListing
        {
            Title = input.Title,
            Description = input.Description,
            Price = input.Price,
            Type = input.Type,
            ListingType = input.ListingType,
            City = input.City,
            Locality = input.Locality,
            Address = input.Address,
            Latitude = input.Latitude,
            Longitude = input.Longitude,
            Bedrooms = input.Bedrooms,
            Bathrooms = input.Bathrooms,
            AreaMarla = input.AreaMarla,
            AreaSqFt = input.AreaSqFt,
            ImageUrlsJson = JsonSerializer.Serialize(input.ImageUrls ?? Array.Empty<string>()),
            IsFeatured = input.IsFeatured,
            AgentId = agentId
        };

        _db.Properties.Add(entity);
        await _db.SaveChangesAsync();

        var documents = DocumentDefaults.For(entity.Type, entity.ListingType);
        for (var i = 0; i < documents.Count; i++)
        {
            _db.DocumentChecklist.Add(new DocumentChecklistItem
            {
                PropertyListingId = entity.Id,
                Name = documents[i],
                SortOrder = i
            });
        }
        await _db.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<PropertyDto?> UpdateAsync(int id, PropertyInputDto input)
    {
        var entity = await _db.Properties.FindAsync(id);
        if (entity is null) return null;

        entity.Title = input.Title;
        entity.Description = input.Description;
        entity.Price = input.Price;
        entity.Type = input.Type;
        entity.ListingType = input.ListingType;
        entity.City = input.City;
        entity.Locality = input.Locality;
        entity.Address = input.Address;
        entity.Latitude = input.Latitude;
        entity.Longitude = input.Longitude;
        entity.Bedrooms = input.Bedrooms;
        entity.Bathrooms = input.Bathrooms;
        entity.AreaMarla = input.AreaMarla;
        entity.AreaSqFt = input.AreaSqFt;
        entity.ImageUrlsJson = JsonSerializer.Serialize(input.ImageUrls ?? Array.Empty<string>());
        entity.IsFeatured = input.IsFeatured;

        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.Properties.FindAsync(id);
        if (entity is null) return false;
        _db.Properties.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<PropertyDto>> GetForAiContextAsync(int take = 24)
    {
        var items = await _db.Properties.AsNoTracking()
            .Where(p => p.Status == ListingStatus.Active)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.CreatedAt)
            .Take(take)
            .ToListAsync();

        return items.Select(p => ToDto(p)).ToList();
    }

    private static string[] ParseImages(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
