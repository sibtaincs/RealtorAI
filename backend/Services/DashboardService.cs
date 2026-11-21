using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(string userId);
    Task<PagedResultDto<LeadDto>> GetLeadsAsync(
        LeadStatus? status, string? temperature, int page, int pageSize);
    Task<IReadOnlyList<ListingPerformanceDto>> GetListingsAsync();
}

/// <summary>
/// Aggregates existing leads, listings, documents and notifications into the
/// agent's daily workspace. All lead scores come from ILeadService so the
/// dashboard and the leads inbox always agree.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly ILeadService _leads;

    public DashboardService(AppDbContext db, ILeadService leads)
    {
        _db = db;
        _leads = leads;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(string userId)
    {
        var leads = await _leads.GetAllAsync(null);
        var cutoff = DateTime.UtcNow.AddDays(-7);

        var newLeads = await _db.Leads.AsNoTracking()
            .CountAsync(l => l.CreatedAt >= cutoff);
        var activeListings = await _db.Properties.AsNoTracking()
            .CountAsync(p => p.Status == ListingStatus.Active);

        var docCounts = await _db.DocumentChecklist.AsNoTracking()
            .GroupBy(d => d.PropertyListingId)
            .Select(g => new
            {
                Total = g.Count(),
                Verified = g.Count(d => d.IsVerified)
            })
            .ToListAsync();
        var verifiedListings = docCounts.Count(d => IsTrusted(d.Verified, d.Total));

        var unread = string.IsNullOrWhiteSpace(userId)
            ? 0
            : await _db.Notifications.AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsRead);

        var leadDates = await _db.Leads.AsNoTracking()
            .Select(l => new { l.Id, l.CreatedAt })
            .ToListAsync();
        var firstFollowUps = await _db.FollowUps.AsNoTracking()
            .GroupBy(f => f.LeadId)
            .Select(g => new { LeadId = g.Key, First = g.Min(f => f.CreatedAt) })
            .ToDictionaryAsync(x => x.LeadId, x => x.First);

        var responseHours = leadDates
            .Where(l => firstFollowUps.ContainsKey(l.Id))
            .Select(l => Math.Max(0, (firstFollowUps[l.Id] - l.CreatedAt).TotalHours))
            .ToList();

        return new DashboardSummaryDto(
            newLeads,
            leads.Count,
            leads.Count(l => l.ScoreLabel == "Hot"),
            leads.Count(l => l.ScoreLabel == "Warm"),
            leads.Count(l => l.ScoreLabel == "Cold"),
            activeListings,
            verifiedListings,
            responseHours.Count > 0 ? Math.Round(responseHours.Average(), 1) : 0,
            unread);
    }

    public async Task<PagedResultDto<LeadDto>> GetLeadsAsync(
        LeadStatus? status, string? temperature, int page, int pageSize)
    {
        var leads = await _leads.GetAllAsync(status);

        if (!string.IsNullOrWhiteSpace(temperature))
            leads = leads
                .Where(l => l.ScoreLabel.Equals(temperature, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var ordered = leads
            .OrderByDescending(l => l.Score)
            .ThenByDescending(l => l.CreatedAt)
            .ToList();

        var pageNumber = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);
        var items = ordered.Skip((pageNumber - 1) * size).Take(size).ToList();

        return new PagedResultDto<LeadDto>(items, ordered.Count, pageNumber, size);
    }

    public async Task<IReadOnlyList<ListingPerformanceDto>> GetListingsAsync()
    {
        var rows = await _db.Properties.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.City,
                Status = p.Status.ToString(),
                p.CreatedAt,
                LeadCount = _db.Leads.Count(l => l.PropertyListingId == p.Id),
                Total = _db.DocumentChecklist.Count(d => d.PropertyListingId == p.Id),
                Verified = _db.DocumentChecklist
                    .Count(d => d.PropertyListingId == p.Id && d.IsVerified)
            })
            .ToListAsync();

        return rows.Select(r => new ListingPerformanceDto(
            r.Id,
            r.Title,
            r.City,
            r.Status,
            r.LeadCount,
            r.Verified,
            r.Total,
            IsTrusted(r.Verified, r.Total),
            (int)Math.Max(0, (DateTime.UtcNow - r.CreatedAt).TotalDays)))
            .ToList();
    }

    // Same rule as the public verified badge: at least 3 documents and 60% verified.
    private static bool IsTrusted(int verified, int total)
        => total > 0 && verified >= Math.Max(3, total * 0.6);
}
