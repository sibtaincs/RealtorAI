namespace RealEstateAI.Api.Models;

public class SavedSearch
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string QueryJson { get; set; } = "{}";
    public bool AlertEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAlertAt { get; set; }
}

public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DocumentChecklistItem
{
    public int Id { get; set; }
    public int PropertyListingId { get; set; }
    public PropertyListing PropertyListing { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public int SortOrder { get; set; }
}

public class FollowUp
{
    public int Id { get; set; }
    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;

    public string Channel { get; set; } = "WhatsApp";
    public string Message { get; set; } = string.Empty;
    public string? ExternalUrl { get; set; }
    public bool Sent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class NotificationPreference
{
    public string UserId { get; set; } = string.Empty;
    public bool InAppEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; }
    public bool WhatsAppEnabled { get; set; }
}

public class NotificationDelivery
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int SavedSearchId { get; set; }
    public int PropertyListingId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
