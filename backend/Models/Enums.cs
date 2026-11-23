namespace RealEstateAI.Api.Models;

public enum PropertyType
{
    House,
    Plot,
    Apartment,
    Shop,
    Office,
    Villa,
    Farmhouse
}

public enum ListingType
{
    Sale,
    Rent
}

public enum ListingStatus
{
    Active,
    Pending,
    Sold,
    Rented
}

public enum LeadStatus
{
    New,
    Contacted,
    Interested,
    Closed,
    Lost
}

public enum ReviewModerationStatus
{
    Pending,
    Approved,
    Rejected
}

public enum ReportStatus
{
    Open,
    Resolved,
    Dismissed
}

public enum DealerApprovalStatus
{
    Pending,
    Approved,
    Rejected
}

public enum LeaseStatus
{
    Active,
    Terminated,
    Expired
}
