using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;
using System.Text.Json;

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
    Task<ContentItemDto?> GetAsync(string kind, int id);
    Task<ContentItemDto> CreateAsync(ContentUpsertDto input);
    Task<ContentItemDto?> UpdateAsync(string kind, int id, ContentUpsertDto input);
    Task<bool> DeleteAsync(string kind, int id);
}

public class ContentService : IContentService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private static readonly JsonSerializerOptions CamelOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public ContentService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<ContentItemDto>> ListAsync(string? kind)
    {
        var listKey = $"content:list:{kind ?? "all"}";
        if (_cache.TryGetValue<List<ContentItemDto>>(listKey, out var cached) && cached is not null) return cached;
        var items = await ListCoreAsync(kind);
        _cache.Set(listKey, items, CacheTtl);
        return items;
    }

    private async Task<IReadOnlyList<ContentItemDto>> ListCoreAsync(string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            var all = new List<ContentItemDto>();
            all.AddRange(await ListTypedAsync("project", _db.ProjectContents, e => new ProjectContentDto(e.Name, e.Developer, e.City, e.Locality, e.Type, e.PriceFrom, e.PriceTo, e.Status), e => e.Id, e => e.UpdatedAt));
            all.AddRange(await ListTypedAsync("agency", _db.AgencyContents, e => new AgencyContentDto(e.Name, e.City, e.Agents, e.Listings, e.Rating, e.Verified, e.Specialty), e => e.Id, e => e.UpdatedAt));
            all.AddRange(await ListTypedAsync("society", _db.SocietyContents, e => new SocietyContentDto(e.Name, e.City, e.PlotSizes, e.PriceFrom, e.Status), e => e.Id, e => e.UpdatedAt));
            all.AddRange(await ListTypedAsync("land-record", _db.LandRecordContents, e => new LandRecordContentDto(e.Plot, e.City, e.Owner, e.Area, e.Status), e => e.Id, e => e.UpdatedAt));
            all.AddRange(await ListTypedAsync("article", _db.ArticleContents, e => new ArticleContentDto(e.Title, e.Category, e.Summary, e.Date), e => e.Id, e => e.UpdatedAt));
            all.AddRange(await ListTypedAsync("area-guide", _db.AreaGuideContents, e => new AreaGuideContentDto(e.City, e.Name, e.Summary, e.PricePerSqFt, e.Safety, e.Commute, e.Schools), e => e.Id, e => e.UpdatedAt));
            all.AddRange(await ListTypedAsync("area-trend", _db.AreaTrendContents, e => new AreaTrendContentDto(e.City, e.Area, e.PriceChangePct, e.SearchChangePct), e => e.Id, e => e.UpdatedAt));
            all.AddRange(await ListTypedAsync("price-index", _db.PriceIndexContents, e => new PriceIndexContentDto(e.City, e.Index, e.YearChangePct, e.MedianPrice), e => e.Id, e => e.UpdatedAt));
            return all;
        }

        return kind.ToLowerInvariant() switch
        {
            "project" => await ListTypedAsync("project", _db.ProjectContents, e => new ProjectContentDto(e.Name, e.Developer, e.City, e.Locality, e.Type, e.PriceFrom, e.PriceTo, e.Status), e => e.Id, e => e.UpdatedAt),
            "agency" => await ListTypedAsync("agency", _db.AgencyContents, e => new AgencyContentDto(e.Name, e.City, e.Agents, e.Listings, e.Rating, e.Verified, e.Specialty), e => e.Id, e => e.UpdatedAt),
            "society" => await ListTypedAsync("society", _db.SocietyContents, e => new SocietyContentDto(e.Name, e.City, e.PlotSizes, e.PriceFrom, e.Status), e => e.Id, e => e.UpdatedAt),
            "land-record" => await ListTypedAsync("land-record", _db.LandRecordContents, e => new LandRecordContentDto(e.Plot, e.City, e.Owner, e.Area, e.Status), e => e.Id, e => e.UpdatedAt),
            "article" => await ListTypedAsync("article", _db.ArticleContents, e => new ArticleContentDto(e.Title, e.Category, e.Summary, e.Date), e => e.Id, e => e.UpdatedAt),
            "area-guide" => await ListTypedAsync("area-guide", _db.AreaGuideContents, e => new AreaGuideContentDto(e.City, e.Name, e.Summary, e.PricePerSqFt, e.Safety, e.Commute, e.Schools), e => e.Id, e => e.UpdatedAt),
            "area-trend" => await ListTypedAsync("area-trend", _db.AreaTrendContents, e => new AreaTrendContentDto(e.City, e.Area, e.PriceChangePct, e.SearchChangePct), e => e.Id, e => e.UpdatedAt),
            "price-index" => await ListTypedAsync("price-index", _db.PriceIndexContents, e => new PriceIndexContentDto(e.City, e.Index, e.YearChangePct, e.MedianPrice), e => e.Id, e => e.UpdatedAt),
            _ => Array.Empty<ContentItemDto>()
        };
    }

    private async Task<IReadOnlyList<ContentItemDto>> ListTypedAsync<T, TDto>(string kind, IQueryable<T> query, Func<T, TDto> map, Func<T, int> id, Func<T, DateTime> updated) where T : ContentRecord
    {
        var items = await query.AsNoTracking().OrderBy(e => e.Id).ToListAsync();
        return items.Select(e => new ContentItemDto(id(e), kind, JsonSerializer.Serialize(map(e), CamelOpts), updated(e))).ToList();
    }

    public async Task<ContentItemDto?> GetAsync(string kind, int id)
    {
        var getKey = $"content:get:{kind}:{id}";
        if (_cache.TryGetValue<ContentItemDto>(getKey, out var cached) && cached is not null) return cached;

        var entity = await GetCoreAsync(kind, id);
        if (entity is null) return null;
        _cache.Set(getKey, entity, CacheTtl);
        return entity;
    }

    public async Task<ContentItemDto?> GetCoreAsync(string kind, int id)
    {
        return kind.ToLowerInvariant() switch
        {
            "project" => await GetTypedAsync("project", _db.ProjectContents, id, e => new ProjectContentDto(e.Name, e.Developer, e.City, e.Locality, e.Type, e.PriceFrom, e.PriceTo, e.Status), e => e.UpdatedAt),
            "agency" => await GetTypedAsync("agency", _db.AgencyContents, id, e => new AgencyContentDto(e.Name, e.City, e.Agents, e.Listings, e.Rating, e.Verified, e.Specialty), e => e.UpdatedAt),
            "society" => await GetTypedAsync("society", _db.SocietyContents, id, e => new SocietyContentDto(e.Name, e.City, e.PlotSizes, e.PriceFrom, e.Status), e => e.UpdatedAt),
            "land-record" => await GetTypedAsync("land-record", _db.LandRecordContents, id, e => new LandRecordContentDto(e.Plot, e.City, e.Owner, e.Area, e.Status), e => e.UpdatedAt),
            "article" => await GetTypedAsync("article", _db.ArticleContents, id, e => new ArticleContentDto(e.Title, e.Category, e.Summary, e.Date), e => e.UpdatedAt),
            "area-guide" => await GetTypedAsync("area-guide", _db.AreaGuideContents, id, e => new AreaGuideContentDto(e.City, e.Name, e.Summary, e.PricePerSqFt, e.Safety, e.Commute, e.Schools), e => e.UpdatedAt),
            "area-trend" => await GetTypedAsync("area-trend", _db.AreaTrendContents, id, e => new AreaTrendContentDto(e.City, e.Area, e.PriceChangePct, e.SearchChangePct), e => e.UpdatedAt),
            "price-index" => await GetTypedAsync("price-index", _db.PriceIndexContents, id, e => new PriceIndexContentDto(e.City, e.Index, e.YearChangePct, e.MedianPrice), e => e.UpdatedAt),
            _ => null
        };
    }

    private async Task<ContentItemDto?> GetTypedAsync<T, TDto>(string kind, DbSet<T> set, int id, Func<T, TDto> map, Func<T, DateTime> updated) where T : ContentRecord
    {
        var entity = await set.FindAsync(id);
        if (entity is null) return null;
        return new ContentItemDto(entity.Id, kind, JsonSerializer.Serialize(map(entity), CamelOpts), updated(entity));
    }

    public async Task<ContentItemDto> CreateAsync(ContentUpsertDto input)
    {
        var kind = input.Kind.Trim().ToLowerInvariant();
        _cache.Remove($"content:list:{kind}");
        return await CreateCoreAsync(input);
    }

    private async Task<ContentItemDto> CreateCoreAsync(ContentUpsertDto input)
    {
        var kind = input.Kind.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        switch (kind)
        {
            case "project":
            {
                var dto = JsonSerializer.Deserialize<ProjectContentDto>(input.PayloadJson, JsonOpts)!;
                var e = new ProjectContent { Name = dto.Name, Developer = dto.Developer, City = dto.City, Locality = dto.Locality, Type = dto.Type, PriceFrom = dto.PriceFrom, PriceTo = dto.PriceTo, Status = dto.Status, UpdatedAt = now };
                _db.ProjectContents.Add(e);
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            case "agency":
            {
                var dto = JsonSerializer.Deserialize<AgencyContentDto>(input.PayloadJson, JsonOpts)!;
                var e = new AgencyContent { Name = dto.Name, City = dto.City, Agents = dto.Agents, Listings = dto.Listings, Rating = dto.Rating, Verified = dto.Verified, Specialty = dto.Specialty, UpdatedAt = now };
                _db.AgencyContents.Add(e);
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            case "society":
            {
                var dto = JsonSerializer.Deserialize<SocietyContentDto>(input.PayloadJson, JsonOpts)!;
                var e = new SocietyContent { Name = dto.Name, City = dto.City, PlotSizes = dto.PlotSizes, PriceFrom = dto.PriceFrom, Status = dto.Status, UpdatedAt = now };
                _db.SocietyContents.Add(e);
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            case "land-record":
            {
                var dto = JsonSerializer.Deserialize<LandRecordContentDto>(input.PayloadJson, JsonOpts)!;
                var e = new LandRecordContent { Plot = dto.Plot, City = dto.City, Owner = dto.Owner, Area = dto.Area, Status = dto.Status, UpdatedAt = now };
                _db.LandRecordContents.Add(e);
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            case "article":
            {
                var dto = JsonSerializer.Deserialize<ArticleContentDto>(input.PayloadJson, JsonOpts)!;
                var e = new ArticleContent { Title = dto.Title, Category = dto.Category, Summary = dto.Summary, Date = dto.Date, UpdatedAt = now };
                _db.ArticleContents.Add(e);
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            case "area-guide":
            {
                var dto = JsonSerializer.Deserialize<AreaGuideContentDto>(input.PayloadJson, JsonOpts)!;
                var e = new AreaGuideContent { City = dto.City, Name = dto.Name, Summary = dto.Summary, PricePerSqFt = dto.PricePerSqFt, Safety = dto.Safety, Commute = dto.Commute, Schools = dto.Schools, UpdatedAt = now };
                _db.AreaGuideContents.Add(e);
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            case "area-trend":
            {
                var dto = JsonSerializer.Deserialize<AreaTrendContentDto>(input.PayloadJson, JsonOpts)!;
                var e = new AreaTrendContent { City = dto.City, Area = dto.Area, PriceChangePct = dto.PriceChangePct, SearchChangePct = dto.SearchChangePct, UpdatedAt = now };
                _db.AreaTrendContents.Add(e);
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            case "price-index":
            {
                var dto = JsonSerializer.Deserialize<PriceIndexContentDto>(input.PayloadJson, JsonOpts)!;
                var e = new PriceIndexContent { City = dto.City, Index = dto.Index, YearChangePct = dto.YearChangePct, MedianPrice = dto.MedianPrice, UpdatedAt = now };
                _db.PriceIndexContents.Add(e);
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            default:
                throw new InvalidOperationException("Unknown content kind.");
        }
    }

    public async Task<ContentItemDto?> UpdateAsync(string kind, int id, ContentUpsertDto input)
    {
        var listKey = $"content:list:{kind.Trim().ToLowerInvariant()}";
        var getKey = $"content:get:{kind.Trim().ToLowerInvariant()}:{id}";
        _cache.Remove(getKey);
        _cache.Remove(listKey);
        kind = kind.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        switch (kind)
        {
            case "project":
            {
                var e = await _db.ProjectContents.FindAsync(id);
                if (e is null) return null;
                var dto = JsonSerializer.Deserialize<ProjectContentDto>(input.PayloadJson, JsonOpts)!;
                e.Name = dto.Name; e.Developer = dto.Developer; e.City = dto.City; e.Locality = dto.Locality; e.Type = dto.Type; e.PriceFrom = dto.PriceFrom; e.PriceTo = dto.PriceTo; e.Status = dto.Status; e.UpdatedAt = now;
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            case "agency":
            {
                var e = await _db.AgencyContents.FindAsync(id);
                if (e is null) return null;
                var dto = JsonSerializer.Deserialize<AgencyContentDto>(input.PayloadJson, JsonOpts)!;
                e.Name = dto.Name; e.City = dto.City; e.Agents = dto.Agents; e.Listings = dto.Listings; e.Rating = dto.Rating; e.Verified = dto.Verified; e.Specialty = dto.Specialty; e.UpdatedAt = now;
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            case "society":
            {
                var e = await _db.SocietyContents.FindAsync(id);
                if (e is null) return null;
                var dto = JsonSerializer.Deserialize<SocietyContentDto>(input.PayloadJson, JsonOpts)!;
                e.Name = dto.Name; e.City = dto.City; e.PlotSizes = dto.PlotSizes; e.PriceFrom = dto.PriceFrom; e.Status = dto.Status; e.UpdatedAt = now;
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            case "land-record":
            {
                var e = await _db.LandRecordContents.FindAsync(id);
                if (e is null) return null;
                var dto = JsonSerializer.Deserialize<LandRecordContentDto>(input.PayloadJson, JsonOpts)!;
                e.Plot = dto.Plot; e.City = dto.City; e.Owner = dto.Owner; e.Area = dto.Area; e.Status = dto.Status; e.UpdatedAt = now;
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            case "article":
            {
                var e = await _db.ArticleContents.FindAsync(id);
                if (e is null) return null;
                var dto = JsonSerializer.Deserialize<ArticleContentDto>(input.PayloadJson, JsonOpts)!;
                e.Title = dto.Title; e.Category = dto.Category; e.Summary = dto.Summary; e.Date = dto.Date; e.UpdatedAt = now;
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            case "area-guide":
            {
                var e = await _db.AreaGuideContents.FindAsync(id);
                if (e is null) return null;
                var dto = JsonSerializer.Deserialize<AreaGuideContentDto>(input.PayloadJson, JsonOpts)!;
                e.City = dto.City; e.Name = dto.Name; e.Summary = dto.Summary; e.PricePerSqFt = dto.PricePerSqFt; e.Safety = dto.Safety; e.Commute = dto.Commute; e.Schools = dto.Schools; e.UpdatedAt = now;
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            case "area-trend":
            {
                var e = await _db.AreaTrendContents.FindAsync(id);
                if (e is null) return null;
                var dto = JsonSerializer.Deserialize<AreaTrendContentDto>(input.PayloadJson, JsonOpts)!;
                e.City = dto.City; e.Area = dto.Area; e.PriceChangePct = dto.PriceChangePct; e.SearchChangePct = dto.SearchChangePct; e.UpdatedAt = now;
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            case "price-index":
            {
                var e = await _db.PriceIndexContents.FindAsync(id);
                if (e is null) return null;
                var dto = JsonSerializer.Deserialize<PriceIndexContentDto>(input.PayloadJson, JsonOpts)!;
                e.City = dto.City; e.Index = dto.Index; e.YearChangePct = dto.YearChangePct; e.MedianPrice = dto.MedianPrice; e.UpdatedAt = now;
                await _db.SaveChangesAsync();
                return new ContentItemDto(e.Id, kind, input.PayloadJson, e.UpdatedAt);
            }
            default:
                return null;
        }
    }

    public async Task<bool> DeleteAsync(string kind, int id)
    {
        var listKey = $"content:list:{kind.Trim().ToLowerInvariant()}";
        _cache.Remove(listKey);
        kind = kind.Trim().ToLowerInvariant();
        switch (kind)
        {
            case "project":
            {
                var e = await _db.ProjectContents.FindAsync(id);
                if (e is null) return false;
                _db.ProjectContents.Remove(e);
                await _db.SaveChangesAsync();
                return true;
            }
            case "agency":
            {
                var e = await _db.AgencyContents.FindAsync(id);
                if (e is null) return false;
                _db.AgencyContents.Remove(e);
                await _db.SaveChangesAsync();
                return true;
            }
            case "society":
            {
                var e = await _db.SocietyContents.FindAsync(id);
                if (e is null) return false;
                _db.SocietyContents.Remove(e);
                await _db.SaveChangesAsync();
                return true;
            }
            case "land-record":
            {
                var e = await _db.LandRecordContents.FindAsync(id);
                if (e is null) return false;
                _db.LandRecordContents.Remove(e);
                await _db.SaveChangesAsync();
                return true;
            }
            case "article":
            {
                var e = await _db.ArticleContents.FindAsync(id);
                if (e is null) return false;
                _db.ArticleContents.Remove(e);
                await _db.SaveChangesAsync();
                return true;
            }
            case "area-guide":
            {
                var e = await _db.AreaGuideContents.FindAsync(id);
                if (e is null) return false;
                _db.AreaGuideContents.Remove(e);
                await _db.SaveChangesAsync();
                return true;
            }
            case "area-trend":
            {
                var e = await _db.AreaTrendContents.FindAsync(id);
                if (e is null) return false;
                _db.AreaTrendContents.Remove(e);
                await _db.SaveChangesAsync();
                return true;
            }
            case "price-index":
            {
                var e = await _db.PriceIndexContents.FindAsync(id);
                if (e is null) return false;
                _db.PriceIndexContents.Remove(e);
                await _db.SaveChangesAsync();
                return true;
            }
            default:
                return false;
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private async Task MigrateLegacyAsync()
    {
        // no-op placeholder; seeding handles fresh data
    }
}