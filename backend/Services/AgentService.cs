using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;

namespace RealEstateAI.Api.Services;

public interface IAgentService
{
    Task<AgentProfileDto?> GetAsync(string userId);
}

public class AgentService : IAgentService
{
    private readonly AppDbContext _db;

    public AgentService(AppDbContext db)
    {
        _db = db;
    }

    public Task<AgentProfileDto?> GetAsync(string userId)
        => _db.AgentProfiles.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new AgentProfileDto(
                p.UserId, p.User.FullName, p.User.AgencyName ?? string.Empty, p.Bio,
                p.Phone, p.PhotoUrl, p.Verified, p.RatingAvg, p.RatingCount))
            .FirstOrDefaultAsync();
}
