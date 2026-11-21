namespace RealEstateAI.Api.Models;

public abstract class ContentRecord
{
    public int Id { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ProjectContent : ContentRecord
{
    public string Name { get; set; } = string.Empty;
    public string Developer { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Locality { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal PriceFrom { get; set; }
    public decimal PriceTo { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AgencyContent : ContentRecord
{
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Agents { get; set; }
    public int Listings { get; set; }
    public double Rating { get; set; }
    public bool Verified { get; set; }
    public string Specialty { get; set; } = string.Empty;
}

public class SocietyContent : ContentRecord
{
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PlotSizes { get; set; } = string.Empty;
    public decimal PriceFrom { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class LandRecordContent : ContentRecord
{
    public string Plot { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ArticleContent : ContentRecord
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
}

public class AreaGuideContent : ContentRecord
{
    public string City { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public decimal PricePerSqFt { get; set; }
    public string Safety { get; set; } = string.Empty;
    public string Commute { get; set; } = string.Empty;
    public string Schools { get; set; } = string.Empty;
}

public class AreaTrendContent : ContentRecord
{
    public string City { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public double PriceChangePct { get; set; }
    public double SearchChangePct { get; set; }
}

public class PriceIndexContent : ContentRecord
{
    public string City { get; set; } = string.Empty;
    public double Index { get; set; }
    public double YearChangePct { get; set; }
    public decimal MedianPrice { get; set; }
}