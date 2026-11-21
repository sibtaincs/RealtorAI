namespace RealEstateAI.Api.Models;

public class ContentItem
{
    public int Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
