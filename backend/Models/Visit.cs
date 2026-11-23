namespace RealEstateAI.Api.Models;

public class Visit
{
    public int Id { get; set; }
    public int PropertyListingId { get; set; }
    public PropertyListing PropertyListing { get; set; } = null!;

    public string AgentId { get; set; } = string.Empty;
    public ApplicationUser? Agent { get; set; }

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public DateTime ScheduledAt { get; set; }
    public VisitStatus Status { get; set; } = VisitStatus.Scheduled;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
