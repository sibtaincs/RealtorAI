using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

/// <summary>
/// Deterministic duplicate / suspicious-listing detection. Runs without an AI
/// key so every seller and buyer gets the same protection in local mode too.
/// </summary>
public interface ITrustService
{
    Task<DuplicateCheckResponseDto> CheckDraftAsync(DuplicateCheckInputDto input);
    Task<RiskReportDto> GetReportAsync(int propertyId);
}

public class TrustService : ITrustService
{
    private readonly AppDbContext _db;

    public TrustService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DuplicateCheckResponseDto> CheckDraftAsync(DuplicateCheckInputDto input)
    {
        var others = await _db.Properties.AsNoTracking()
            .Where(p => p.Status == ListingStatus.Active)
            .ToListAsync();

        var draftTokens = Tokens(input.Title);
        var matches = new List<DuplicateMatchDto>();

        foreach (var p in others)
        {
            var reasons = new List<string>();
            var similarity = Similarity(draftTokens, Tokens(p.Title));
            var sameCity = SamePlace(input.City, p.City);
            var sameLocality = sameCity && SamePlace(input.Locality, p.Locality);
            var sameType = p.Type == input.Type;
            var closeArea = input.AreaMarla.HasValue && p.AreaMarla.HasValue &&
                            Math.Abs(input.AreaMarla.Value - p.AreaMarla.Value) <= 1;

            if (similarity >= 50) reasons.Add($"{similarity}% same title");
            if (sameLocality) reasons.Add("same locality");
            else if (sameCity) reasons.Add("same city");
            if (sameType) reasons.Add("same property type");
            if (closeArea) reasons.Add("same area");

            var score = similarity;
            if (sameLocality) score += 25;
            else if (sameCity) score += 10;
            if (sameType) score += 10;
            if (closeArea) score += 10;
            score = Math.Min(100, score);

            if (score >= 60 && reasons.Count >= 2)
                matches.Add(new DuplicateMatchDto(p.Id, p.Title, score, reasons));
        }

        matches = matches.OrderByDescending(m => m.Similarity).Take(5).ToList();

        var warnings = new List<string>();
        var comps = others.Where(p =>
                p.Price > 0 && SamePlace(input.City, p.City) && p.Type == input.Type &&
                p.AreaMarla.HasValue && p.AreaMarla > 0 &&
                (!input.AreaMarla.HasValue || Math.Abs(p.AreaMarla.Value - input.AreaMarla.Value) <= 3))
            .ToList();

        if (comps.Count >= 3)
        {
            var medianPpm = Median(comps.Select(p => p.Price / p.AreaMarla!.Value).ToList());
            var draftPpm = input.Price / Math.Max(1, input.AreaMarla ?? 1);
            var ratio = draftPpm / Math.Max(1, medianPpm);

            if (ratio < 0.5m)
                warnings.Add($"Price market se {Math.Round((1 - ratio) * 100)}% kam hai — buyer ko shak ho sakta hai.");
            else if (ratio > 2m)
                warnings.Add($"Price market se {Math.Round((ratio - 1) * 100)}% zyada hai — listing slow chal sakti hai.");
        }

        if (input.ImageUrls is null || input.ImageUrls.Length == 0)
            warnings.Add("Koi photo nahi — listings with photos zyada clicks leti hain.");
        if (string.IsNullOrWhiteSpace(input.Locality))
            warnings.Add("Locality missing hai — buyers filter mein aapki property nahi dikhegi.");
        if (draftTokens.Count < 2)
            warnings.Add("Title bahut chota hai — kuch details add karein.");

        var riskScore = Math.Min(100,
            (matches.Count > 0 ? matches.Max(m => m.Similarity) : 0) + warnings.Count * 15);
        var risk = matches.Any(m => m.Similarity >= 80) ||
                   warnings.Any(w => w.Contains("kam hai"))
            ? "High"
            : riskScore >= 45 || matches.Count > 0 ? "Medium" : "Low";

        var advice = risk switch
        {
            "High" => "⚠ Ye listing existing listings se milti-julti hai ya price suspicious hai. Details check karke publish karein.",
            "Medium" => "Kuch points improve kar sakte hain. Neeche diye gaye warnings dekh lein.",
            _ => "✅ Koi duplicate ya price red flag nahi mila. Listing publish karne ke liye ready hai."
        };

        return new DuplicateCheckResponseDto(risk, riskScore, matches, warnings, advice);
    }

    public async Task<RiskReportDto> GetReportAsync(int propertyId)
    {
        var property = await _db.Properties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == propertyId);
        if (property is null)
            return new RiskReportDto(0, "Unknown", "-", new[] { "Property nahi mili." }, "Verification unavailable.");

        var city = property.City ?? string.Empty;
        var targetMarla = property.AreaMarla ?? 0;
        var comps = await _db.Properties.AsNoTracking()
            .Where(p => p.Id != propertyId && p.Status == ListingStatus.Active &&
                        p.City.Contains(city) && p.Type == property.Type &&
                        p.Price > 0 && p.AreaMarla.HasValue && p.AreaMarla > 0 &&
                        p.AreaMarla >= targetMarla - 3 && p.AreaMarla <= targetMarla + 3)
            .Select(p => p.Price / (p.AreaMarla ?? 1))
            .ToListAsync();

        var flags = new List<string>();
        var medianPerMarla = comps.Count > 0 ? Median(comps) : 0m;

        if (comps.Count >= 3 && property.AreaMarla is > 0 && medianPerMarla > 0)
        {
            var expected = medianPerMarla * property.AreaMarla.Value;
            var ratio = property.Price / expected;
            if (ratio < 0.7m)
                flags.Add($"💰 Price similar listings se {Math.Round((1 - ratio) * 100)}% kam hai. Documents zaroor check karein.");
            else if (ratio > 1.4m)
                flags.Add($"💰 Price similar listings se {Math.Round((ratio - 1) * 100)}% zyada hai. Negotiation possible hai.");
            else
                flags.Add("✅ Price area ke hisaab se reasonable hai.");
        }

        var imageCount = ParseImageCount(property.ImageUrlsJson);
        if (imageCount == 0) flags.Add("📷 Is listing mein koi photo nahi hai.");

        var verification = await _db.DocumentChecklist
            .AsNoTracking()
            .Where(d => d.PropertyListingId == propertyId)
            .GroupBy(d => d.PropertyListingId)
            .Select(g => new { Verified = g.Count(d => d.IsVerified), Total = g.Count() })
            .FirstOrDefaultAsync();

        if (verification is null || verification.Verified == 0)
            flags.Add("📄 Documents abhi verify nahi huye hain.");
        else if (verification.Verified >= Math.Max(3, verification.Total * 0.6))
            flags.Add($"✅ {verification.Verified}/{verification.Total} documents verified — trusted listing.");
        else
            flags.Add($"📄 {verification.Verified}/{verification.Total} documents verified — aur documents verify karein.");

        var penalties = flags.Count(f => f.StartsWith("💰") || f.StartsWith("📷") || f.StartsWith("📄")) * 20;
        var score = Math.Clamp(100 - penalties, 0, 100);
        var label = score >= 80 ? "Good" : score >= 55 ? "Fair" : "Caution";

        var medianText = medianPerMarla > 0 && property.AreaMarla is > 0
            ? $"PKR {Math.Round(medianPerMarla * property.AreaMarla.Value):N0}"
            : "unavailable";

        var verdict = score >= 80
            ? "Ye listing details aur price ke liye check pass karti hai."
            : "In points ko buyer se pehle confirm karein.";

        return new RiskReportDto(score, label, medianText, flags, verdict);
    }

    private static HashSet<string> Tokens(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "in", "of", "for", "and", "with", "house", "plot", "apartment",
            "villa", "sale", "rent", "phase", "block", "near"
        };
        return new HashSet<string>(
            Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 2 && !stop.Contains(t) && !int.TryParse(t, out _)),
            StringComparer.OrdinalIgnoreCase);
    }

    private static int Similarity(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 || b.Count == 0) return 0;
        var intersection = a.Count(b.Contains);
        var union = a.Count + b.Count - intersection;
        return (int)Math.Round(intersection * 100.0 / Math.Max(1, union));
    }

    private static bool SamePlace(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
        return Normalize(a).Contains(Normalize(b)) || Normalize(b).Contains(Normalize(a));
    }

    private static string Normalize(string value)
    {
        var chars = value.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : ' ')
            .ToArray();
        return string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static decimal Median(IReadOnlyCollection<decimal> values)
    {
        if (values.Count == 0) return 0;
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
    }

    private static int ParseImageCount(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0;
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<string[]>(json)?.Length ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}
