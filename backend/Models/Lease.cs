namespace RealEstateAI.Api.Models;

public class Lease
{
    public int Id { get; set; }
    public int PropertyListingId { get; set; }
    public PropertyListing PropertyListing { get; set; } = null!;

    public string AgentId { get; set; } = string.Empty;
    public ApplicationUser? Agent { get; set; }

    public string TenantName { get; set; } = string.Empty;
    public string? TenantEmail { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public LeaseStatus Status { get; set; } = LeaseStatus.Active;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
