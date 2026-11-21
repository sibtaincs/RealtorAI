using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface IModerationService
{
    Task<IReadOnlyList<ReviewDto>> GetPendingReviewsAsync();
    Task<IReadOnlyList<ReportDto>> GetOpenReportsAsync();
    Task<bool> ModerateReviewAsync(int id, bool approve);
    Task<bool> ResolveReportAsync(int id);
}

public class ModerationService : IModerationService
{
    private readonly AppDbContext _db;

    public ModerationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ReviewDto>> GetPendingReviewsAsync()
        => await _db.Reviews.AsNoTracking()
            .Where(r => r.ModerationStatus == ReviewModerationStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new ReviewDto(
                r.Id, r.PropertyListingId, r.Author.FullName, r.Rating,
                r.Comment, r.ModerationStatus.ToString(), r.CreatedAt))
            .ToListAsync();

    public async Task<IReadOnlyList<ReportDto>> GetOpenReportsAsync()
        => await _db.Reports.AsNoTracking()
            .Where(r => r.Status == ReportStatus.Open)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new ReportDto(
                r.Id, r.PropertyListingId, r.Reporter.FullName, r.Reason,
                r.Status.ToString(), r.CreatedAt))
            .ToListAsync();

    public async Task<bool> ModerateReviewAsync(int id, bool approve)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == id);
        if (review is null) return false;

        review.ModerationStatus = approve
            ? ReviewModerationStatus.Approved
            : ReviewModerationStatus.Rejected;

        // Persist the new status before recalculating so the aggregate query sees it.
        await _db.SaveChangesAsync();
        await RefreshAgentRatingAsync(review.PropertyListingId);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResolveReportAsync(int id)
    {
        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == id);
        if (report is null) return false;

        report.Status = ReportStatus.Resolved;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task RefreshAgentRatingAsync(int propertyId)
    {
        var agentId = await _db.Properties.AsNoTracking()
            .Where(p => p.Id == propertyId).Select(p => p.AgentId)
            .FirstOrDefaultAsync();
        if (string.IsNullOrEmpty(agentId)) return;

        var profile = await _db.AgentProfiles.FirstOrDefaultAsync(p => p.UserId == agentId);
        if (profile is null) return;

        var agentProperties = _db.Properties.Where(p => p.AgentId == agentId).Select(p => p.Id);
        var stats = await _db.Reviews
            .Where(r => r.ModerationStatus == ReviewModerationStatus.Approved &&
                        agentProperties.Contains(r.PropertyListingId))
            .GroupBy(r => 1)
            .Select(g => new { Average = g.Average(r => (double)r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync();

        profile.RatingAvg = stats is null ? 0 : Math.Round(stats.Average, 1);
        profile.RatingCount = stats?.Count ?? 0;
    }
}
