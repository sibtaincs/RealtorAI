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
    DateTime CreatedAt,
    int VerifiedDocuments = 0,
    int TotalDocuments = 0,
    bool IsTrusted = false,
    string? AgentId = null);

public record PropertyInputDto(
    string Title,
    string Description,
    decimal Price,
    string Type,
    string ListingType,
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
    string? Type,
    string? ListingType,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinBeds,
    int? MinMarla,
    int Page = 1,
    int PageSize = 12,
    double? MinLat = null,
    double? MaxLat = null,
    double? MinLng = null,
    double? MaxLng = null,
    double? CenterLat = null,
    double? CenterLng = null,
    double? RadiusKm = null,
    string? AgentId = null,
    string? Sort = null);

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
    IReadOnlyList<string> Roles,
    string ApprovalStatus);

public record DealerApprovalDto(
    string Id,
    string FullName,
    string Email,
    string? AgencyName,
    string ApprovalStatus,
    DateTime CreatedAt);

public record AiChatDto(string Message);

public record AiChatResponseDto(
    string Reply,
    string Mode,
    int[] RecommendedPropertyIds);

public record DescribeDto(
    string Title,
    string Type,
    string City,
    string Locality,
    int? Bedrooms,
    int? Bathrooms,
    int? AreaMarla,
    decimal Price,
    string[]? Features);

public record PriceEstimateDto(
    string Type,
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
    string Type,
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
    string Type,
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

// ---------- Phase 2: OCR verification, trust badges, duplicate/fraud detection ----------

public record OcrScanDto(string Text);

public record OcrScanResultDto(
    int Matched,
    int NewlyVerified,
    IReadOnlyList<string> MatchedNames,
    IReadOnlyList<DocumentItemDto> Items);

public record DuplicateCheckInputDto(
    string? Title,
    string City,
    string Locality,
    string Type,
    string ListingType,
    int? AreaMarla,
    decimal Price,
    string[]? ImageUrls);

public record DuplicateMatchDto(
    int PropertyId,
    string Title,
    int Similarity,
    IReadOnlyList<string> Reasons);

public record DuplicateCheckResponseDto(
    string Risk,
    int Score,
    IReadOnlyList<DuplicateMatchDto> Matches,
    IReadOnlyList<string> Warnings,
    string Advice);

public record RiskReportDto(
    int Score,
    string Label,
    string MarketMedian,
    IReadOnlyList<string> Flags,
    string Verdict);

// ---------- Phase 3: agent dashboard ----------

public record DashboardSummaryDto(
    int NewLeads7Days,
    int TotalLeads,
    int HotLeads,
    int WarmLeads,
    int ColdLeads,
    int ActiveListings,
    int VerifiedListings,
    double AverageFirstResponseHours,
    int UnreadNotifications);

public record ListingPerformanceDto(
    int Id,
    string Title,
    string City,
    string Status,
    int LeadCount,
    int VerifiedDocuments,
    int TotalDocuments,
    bool IsTrusted,
    int DaysListed);

public record NotificationPreferenceDto(
    bool InAppEnabled,
    bool EmailEnabled,
    bool WhatsAppEnabled);

public record UpdateNotificationPreferenceDto(
    bool InAppEnabled,
    bool EmailEnabled,
    bool WhatsAppEnabled);

// ---------- Phase 3: trust, reviews and moderation ----------

public record AgentProfileDto(
    string UserId,
    string FullName,
    string AgencyName,
    string Bio,
    string? Phone,
    string? PhotoUrl,
    bool Verified,
    double RatingAvg,
    int RatingCount);

public record ReviewDto(
    int Id,
    int PropertyListingId,
    string AuthorName,
    int Rating,
    string Comment,
    string ModerationStatus,
    DateTime CreatedAt);

public record CreateReviewDto(
    int PropertyListingId,
    int Rating,
    string Comment);

public record ReportDto(
    int Id,
    int PropertyListingId,
    string ReporterName,
    string Reason,
    string Status,
    DateTime CreatedAt);

public record CreateReportDto(
    int PropertyListingId,
    string Reason);

// ---------- Admin-managed site content ----------

public record ContentItemDto(
    int Id,
    string Kind,
    string PayloadJson,
    DateTime UpdatedAt);

public record ContentUpsertDto(
    string Kind,
    string PayloadJson);

// ---------- Admin-managed master data ----------

public record MasterDataDto(
    int Id,
    string Kind,
    string Value,
    string Label,
    int SortOrder,
    bool IsActive);

public record MasterDataUpsertDto(
    string Kind,
    string Value,
    string Label,
    int SortOrder = 0,
    bool IsActive = true);
