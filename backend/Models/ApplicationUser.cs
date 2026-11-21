using Microsoft.AspNetCore.Identity;

namespace RealEstateAI.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? AgencyName { get; set; }
    public DealerApprovalStatus ApprovalStatus { get; set; } = DealerApprovalStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
