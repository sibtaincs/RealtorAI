using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface ILeaseService
{
    Task<List<LeaseDto>> GetAsync(string agentId);
    Task<LeaseDto?> CreateAsync(string agentId, LeaseInputDto input);
    Task<LeaseDto?> UpdateAsync(int id, string agentId, LeaseInputDto input);
    Task<bool> DeleteAsync(int id, string agentId);
    Task<LeaseDto?> TerminateAsync(int id, string agentId);
}

public class LeaseService : ILeaseService
{
    private readonly AppDbContext _db;

    public LeaseService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<LeaseDto>> GetAsync(string agentId)
    {
        var leases = await _db.Leases
            .AsNoTracking()
            .Where(l => l.AgentId == agentId)
            .Include(l => l.PropertyListing)
            .OrderByDescending(l => l.StartDate)
            .ToListAsync();

        return leases.Select(ToDto).ToList();
    }

    public async Task<LeaseDto?> CreateAsync(string agentId, LeaseInputDto input)
    {
        var property = await _db.Properties.FindAsync(input.PropertyListingId);
        if (property is null || input.EndDate <= input.StartDate) return null;

        var lease = new Lease
        {
            PropertyListingId = input.PropertyListingId,
            AgentId = agentId,
            TenantName = input.TenantName,
            TenantEmail = input.TenantEmail,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            MonthlyRent = input.MonthlyRent,
            Status = LeaseStatus.Active,
            Notes = input.Notes
        };

        _db.Leases.Add(lease);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(lease.Id);
    }

    public async Task<LeaseDto?> UpdateAsync(int id, string agentId, LeaseInputDto input)
    {
        var lease = await _db.Leases.FirstOrDefaultAsync(l => l.Id == id && l.AgentId == agentId);
        if (lease is null) return null;
        if (input.EndDate <= input.StartDate) return null;

        lease.PropertyListingId = input.PropertyListingId;
        lease.TenantName = input.TenantName;
        lease.TenantEmail = input.TenantEmail;
        lease.StartDate = input.StartDate;
        lease.EndDate = input.EndDate;
        lease.MonthlyRent = input.MonthlyRent;
        lease.Notes = input.Notes;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(lease.Id);
    }

    public async Task<bool> DeleteAsync(int id, string agentId)
    {
        var lease = await _db.Leases.FirstOrDefaultAsync(l => l.Id == id && l.AgentId == agentId);
        if (lease is null) return false;

        _db.Leases.Remove(lease);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<LeaseDto?> TerminateAsync(int id, string agentId)
    {
        var lease = await _db.Leases.FirstOrDefaultAsync(l => l.Id == id && l.AgentId == agentId);
        if (lease is null) return null;

        lease.Status = LeaseStatus.Terminated;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(lease.Id);
    }

    private async Task<LeaseDto?> GetByIdAsync(int id)
    {
        var lease = await _db.Leases.AsNoTracking()
            .Include(l => l.PropertyListing)
            .FirstOrDefaultAsync(l => l.Id == id);
        return lease is null ? null : ToDto(lease);
    }

    private static LeaseDto ToDto(Lease lease)
    {
        var status = lease.Status == LeaseStatus.Active && lease.EndDate < DateTime.UtcNow
            ? LeaseStatus.Expired
            : lease.Status;
        return new LeaseDto(
            lease.Id,
            lease.PropertyListingId,
            lease.AgentId,
            lease.TenantName,
            lease.TenantEmail,
            lease.StartDate,
            lease.EndDate,
            lease.MonthlyRent,
            status.ToString(),
            lease.CreatedAt,
            lease.UpdatedAt);
    }
}
