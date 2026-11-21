namespace RealEstateAI.Api.Models;

public class PropertyListing
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Price in PKR. For rentals this is the monthly rent.
    public decimal Price { get; set; }

    // Types are admin-managed master data, stored as their value string.
    public string Type { get; set; } = "House";
    public string ListingType { get; set; } = "Sale";
    public ListingStatus Status { get; set; } = ListingStatus.Active;

    public string City { get; set; } = string.Empty;
    public string Locality { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? AreaMarla { get; set; }
    public int? AreaSqFt { get; set; }

    public string? PlotNumber { get; set; }
    public bool IsReadyForPossession { get; set; }
    public bool InstallmentAvailable { get; set; }
    public string? Flooring { get; set; }
    public string? ElectricityBackup { get; set; }
    public string? View { get; set; }
    public int? BuiltInYear { get; set; }
    public int? ParkingSpaces { get; set; }
    public int? Floors { get; set; }
    public string? AmenitiesJson { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactMobile { get; set; }
    public string? ContactLandline { get; set; }
    public string? ContactWhatsapp { get; set; }

    // JSON array of image URLs to keep the schema simple for MVP.
    public string? ImageUrlsJson { get; set; }

    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? AgentId { get; set; }
    public ApplicationUser? Agent { get; set; }

    public ICollection<Lead> Leads { get; set; } = new List<Lead>();
}
