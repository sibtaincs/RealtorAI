using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Dtos;

public record PropertyDto(
    int Id,
    string Title,
    string Description,
    decimal Price,
    string Type,
    string ListingType,
    string Status,
    string City,
    string Locality,
    string Address,
    double Latitude,
    double Longitude,
    int? Bedrooms,
    int? Bathrooms,
    int? AreaMarla,
    int? AreaSqFt,
    string[] ImageUrls,
    bool IsFeatured,
    DateTime CreatedAt);

public record PropertyInputDto(
    string Title,
    string Description,
    decimal Price,
    PropertyType Type,
    ListingType ListingType,
    string City,
    string Locality,
    string Address,
    double Latitude,
    double Longitude,
    int? Bedrooms,
    int? Bathrooms,
    int? AreaMarla,
    int? AreaSqFt,
    string[]? ImageUrls,
    bool IsFeatured);

public record PropertyQueryDto(
    string? Search,
    string? City,
    string? Locality,
    PropertyType? Type,
    ListingType? ListingType,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinBeds,
    int? MinMarla,
    int Page = 1,
    int PageSize = 12);

public record PagedResultDto<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public record CreateLeadDto(
    string Name,
    string Email,
    string Phone,
    string Message,
    int? PropertyListingId);

public record LeadDto(
    int Id,
    string Name,
    string Email,
    string Phone,
    string Message,
    int? PropertyListingId,
    string? PropertyTitle,
    string Status,
    DateTime CreatedAt,
    int Score,
    string ScoreLabel,
    IReadOnlyList<string> ScoreReasons);

public record RegisterDto(
    string FullName,
    string Email,
    string Password,
    string? AgencyName);

public record LoginDto(string Email, string Password);

public record AuthResponseDto(
    string Token,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles);

public record AiChatDto(string Message);

public record AiChatResponseDto(
    string Reply,
    string Mode,
    int[] RecommendedPropertyIds);

public record DescribeDto(
    string Title,
    PropertyType Type,
    string City,
    string Locality,
    int? Bedrooms,
    int? Bathrooms,
    int? AreaMarla,
    decimal Price,
    string[]? Features);

public record PriceEstimateDto(
    PropertyType Type,
    string City,
    string Locality,
    int? AreaMarla,
    int? Bedrooms,
    int? Bathrooms);

public record PriceEstimateResponseDto(
    decimal EstimatedPrice,
    decimal Low,
    decimal High,
    string Basis);

public record UploadResultDto(string Url, string FileName);

public record DocumentItemDto(
    int Id,
    int PropertyListingId,
    string Name,
    string? Notes,
    bool IsVerified,
    DateTime? VerifiedAt,
    int SortOrder);

public record CreateDocumentDto(string Name, string? Notes);

public record UpdateDocumentDto(string? Notes, bool IsVerified);

public record SavedSearchDto(
    int Id,
    string Name,
    string QueryJson,
    bool AlertEnabled,
    DateTime CreatedAt,
    DateTime? LastAlertAt);

public record CreateSavedSearchDto(string Name, object Query, bool AlertEnabled);

public record UpdateSavedSearchDto(string Name, bool AlertEnabled);

public record NotificationDto(
    int Id,
    string Title,
    string Message,
    string? Link,
    bool IsRead,
    DateTime CreatedAt);

public record FollowUpDto(
    int Id,
    int LeadId,
    string Channel,
    string Message,
    string? ExternalUrl,
    bool Sent,
    DateTime CreatedAt);

public record WhatsAppResultDto(string? ExternalUrl, bool SentDirectly, string Message);

// ---------- Phase 1: AI copilot, pricing, quality, WhatsApp, lead scoring ----------

public record GenerateListingInputDto(
    string? Notes,
    string? City,
    string? Type,
    string? ListingType,
    string[]? ImageUrls);

public record GeneratedListingDto(
    string Title,
    string Description,
    string Type,
    string ListingType,
    string City,
    string Locality,
    int? Bedrooms,
    int? Bathrooms,
    int? AreaMarla,
    decimal Price,
    string[] Highlights,
    string Mode);

public record SellPriceInputDto(
    PropertyType Type,
    string City,
    string Locality,
    int? AreaMarla,
    int TargetDays);

public record SellPriceResponseDto(
    decimal RecommendedPrice,
    decimal Low,
    decimal High,
    int TargetDays,
    string Confidence,
    string Basis,
    string Advice);

public record ListingQualityInputDto(
    string? Title,
    string? Description,
    decimal Price,
    PropertyType Type,
    string? City,
    string? Locality,
    string? Address,
    int? Bedrooms,
    int? Bathrooms,
    int? AreaMarla,
    string[]? ImageUrls,
    int VerifiedDocuments);

public record QualityCheckDto(
    string Name,
    int Points,
    int MaxPoints,
    bool Done,
    string Tip);

public record ListingQualityResponseDto(
    int Score,
    string Label,
    IReadOnlyList<QualityCheckDto> Checks);

public record WhatsAppReplyInputDto(string Message, int? PropertyId);

public record WhatsAppReplyDto(string Reply, int[] RecommendedPropertyIds);
