using System.ComponentModel.DataAnnotations;

namespace RealEstateAI.Api.Dtos;

public record ProjectContentDto(
    [Required] string Name,
    [Required] string Developer,
    [Required] string City,
    [Required] string Locality,
    [Required] string Type,
    [Range(0, double.MaxValue)] decimal PriceFrom,
    [Range(0, double.MaxValue)] decimal PriceTo,
    [Required] string Status);

public record AgencyContentDto(
    [Required] string Name,
    [Required] string City,
    [Range(0, int.MaxValue)] int Agents,
    [Range(0, int.MaxValue)] int Listings,
    [Range(0, 5)] double Rating,
    bool Verified,
    [Required] string Specialty);

public record SocietyContentDto(
    [Required] string Name,
    [Required] string City,
    [Required] string PlotSizes,
    [Range(0, double.MaxValue)] decimal PriceFrom,
    [Required] string Status);

public record LandRecordContentDto(
    [Required] string Plot,
    [Required] string City,
    [Required] string Owner,
    [Required] string Area,
    [Required] string Status);

public record ArticleContentDto(
    [Required] string Title,
    [Required] string Category,
    [Required] string Summary,
    [Required] string Date);

public record AreaGuideContentDto(
    [Required] string City,
    [Required] string Name,
    [Required] string Summary,
    [Range(0, double.MaxValue)] decimal PricePerSqFt,
    [Required] string Safety,
    [Required] string Commute,
    [Required] string Schools);

public record AreaTrendContentDto(
    [Required] string City,
    [Required] string Area,
    double PriceChangePct,
    double SearchChangePct);

public record PriceIndexContentDto(
    [Required] string City,
    double Index,
    double YearChangePct,
    [Range(0, double.MaxValue)] decimal MedianPrice);