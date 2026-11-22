using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface IAuditService
{
    Task LogAsync(string action, string? actorId, string? actorEmail, string? targetType = null, string? targetId = null, string? details = null);
    Task<IReadOnlyList<AuditLog>> ListAsync(int take = 100);
}

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    public AuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(string action, string? actorId, string? actorEmail, string? targetType = null, string? targetId = null, string? details = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            ActorId = actorId,
            ActorEmail = actorEmail,
            TargetType = targetType,
            TargetId = targetId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AuditLog>> ListAsync(int take = 100)
        => await _db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(take).ToListAsync();
}