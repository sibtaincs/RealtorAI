using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using RealEstateAI.Api.Dtos;

namespace RealEstateAI.Api.Services;

public static class ContentPayloadValidator
{
    public static (bool IsValid, string Error) Validate(string kind, string payloadJson)
    {
        Type? dtoType = kind.ToLowerInvariant() switch
        {
            "project" => typeof(ProjectContentDto),
            "agency" => typeof(AgencyContentDto),
            "society" => typeof(SocietyContentDto),
            "land-record" => typeof(LandRecordContentDto),
            "article" => typeof(ArticleContentDto),
            "area-guide" => typeof(AreaGuideContentDto),
            "area-trend" => typeof(AreaTrendContentDto),
            "price-index" => typeof(PriceIndexContentDto),
            _ => null
        };

        if (dtoType is null) return (false, "Unknown content kind.");

        object? dto;
        try
        {
            dto = JsonSerializer.Deserialize(payloadJson, dtoType, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return (false, "Content JSON must be a valid object.");
        }

        if (dto is null) return (false, "Content JSON must be a valid object.");

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, context, results, validateAllProperties: true))
        {
            return (false, string.Join(" ", results.Select(r => r.ErrorMessage)));
        }

        return (true, string.Empty);
    }
}