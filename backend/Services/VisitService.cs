using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface IVisitService
{
    Task<List<VisitDto>> GetAsync(string agentId);
    Task<VisitDto?> CreateAsync(string agentId, VisitInputDto input);
    Task<VisitDto?> UpdateAsync(int id, string agentId, VisitInputDto input);
    Task<bool> DeleteAsync(int id, string agentId);
    Task<VisitDto?> SetStatusAsync(int id, string agentId, string status);
}

public class VisitService : IVisitService
{
    private readonly AppDbContext _db;

    public VisitService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<VisitDto>> GetAsync(string agentId)
    {
        var visits = await _db.Visits
            .AsNoTracking()
            .Where(v => v.AgentId == agentId)
            .Include(v => v.PropertyListing)
            .OrderByDescending(v => v.ScheduledAt)
            .ToListAsync();

        return visits.Select(ToDto).ToList();
    }

    public async Task<VisitDto?> CreateAsync(string agentId, VisitInputDto input)
    {
        if (input.Status != VisitStatus.Scheduled.ToString()) return null;

        var visit = new Visit
        {
            PropertyListingId = input.PropertyListingId,
            AgentId = agentId,
            LeadId = input.LeadId,
            ScheduledAt = input.ScheduledAt,
            Status = VisitStatus.Scheduled,
            Notes = input.Notes
        };

        _db.Visits.Add(visit);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(visit.Id);
    }

    public async Task<VisitDto?> UpdateAsync(int id, string agentId, VisitInputDto input)
    {
        var visit = await _db.Visits.FirstOrDefaultAsync(v => v.Id == id && v.AgentId == agentId);
        if (visit is null) return null;

        visit.PropertyListingId = input.PropertyListingId;
        visit.LeadId = input.LeadId;
        visit.ScheduledAt = input.ScheduledAt;
        visit.Status = Enum.TryParse<VisitStatus>(input.Status, true, out var parsed) ? parsed : VisitStatus.Scheduled;
        visit.Notes = input.Notes;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(visit.Id);
    }

    public async Task<bool> DeleteAsync(int id, string agentId)
    {
        var visit = await _db.Visits.FirstOrDefaultAsync(v => v.Id == id && v.AgentId == agentId);
        if (visit is null) return false;

        _db.Visits.Remove(visit);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<VisitDto?> SetStatusAsync(int id, string agentId, string status)
    {
        var visit = await _db.Visits.FirstOrDefaultAsync(v => v.Id == id && v.AgentId == agentId);
        if (visit is null) return null;

        if (!Enum.TryParse<VisitStatus>(status, true, out var parsed)) return null;
        visit.Status = parsed;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(visit.Id);
    }

    private async Task<VisitDto?> GetByIdAsync(int id)
    {
        var visit = await _db.Visits.AsNoTracking()
            .Include(v => v.PropertyListing)
            .FirstOrDefaultAsync(v => v.Id == id);
        return visit is null ? null : ToDto(visit);
    }

    private static VisitDto ToDto(Visit visit)
    {
        return new VisitDto(
            visit.Id,
            visit.PropertyListingId,
            visit.AgentId,
            visit.LeadId,
            visit.Status.ToString(),
            visit.ScheduledAt,
            visit.Notes,
            visit.CreatedAt,
            visit.UpdatedAt);
    }
}
