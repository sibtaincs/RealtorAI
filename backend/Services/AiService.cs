using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface IAiService
{
    Task<AiChatResponseDto> ChatAsync(string message);
    Task<string> GenerateDescriptionAsync(DescribeDto input);
    Task<PriceEstimateResponseDto> EstimatePriceAsync(PriceEstimateDto input);
    Task<GeneratedListingDto> GenerateListingAsync(GenerateListingInputDto input);
    Task<SellPriceResponseDto> SellPriceAsync(SellPriceInputDto input);
    Task<ListingQualityResponseDto> QualityAsync(ListingQualityInputDto input);
    Task<WhatsAppReplyDto> WhatsAppReplyAsync(WhatsAppReplyInputDto input);
}

/// <summary>
/// AI agent service. Uses an OpenAI-compatible API when Ai:ApiKey is configured,
/// otherwise falls back to a built-in rule-based agent so the app works out of the box.
/// </summary>
public class AiService : IAiService
{
    private static readonly string[] KnownCities =
    {
        "Karachi", "Lahore", "Islamabad", "Rawalpindi", "Multan",
        "Faisalabad", "Gujranwala", "Peshawar", "Hyderabad", "Quetta"
    };

    private readonly IPropertyService _properties;
    private readonly HttpClient _http;
    private readonly IWebHostEnvironment _env;
    private readonly string? _apiKey;
    private readonly string _endpoint;
    private readonly string _model;

    public AiService(IPropertyService properties, HttpClient http, IWebHostEnvironment env, IConfiguration config)
    {
        _properties = properties;
        _http = http;
        _env = env;
        _apiKey = config["Ai:ApiKey"];
        _endpoint = config["Ai:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
        _model = config["Ai:Model"] ?? "gpt-4o-mini";
    }

    public async Task<AiChatResponseDto> ChatAsync(string message)
    {
        var listings = await _properties.GetForAiContextAsync(24);
        var context = BuildContext(listings);

        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            var system =
                "You are 'RealEstate AI', a helpful real estate agent for the Pakistani property market. " +
                "Answer in friendly Roman Urdu mixed with English. Use ONLY the property listings provided below. " +
                "Never invent properties, prices or facts. Show prices in lakh/crore PKR. " +
                "Be concise (max 120 words). Suggest the user book a visit or send an inquiry when relevant.\n\n" +
                "AVAILABLE LISTINGS:\n" + context;

            var reply = await CallModelAsync(system, message);
            if (!string.IsNullOrWhiteSpace(reply))
            {
                var ids = FindMatchingIds(message, listings).Take(3).ToArray();
                return new AiChatResponseDto(reply, "ai", ids);
            }
        }

        return LocalChat(message, listings);
    }

    public async Task<string> GenerateDescriptionAsync(DescribeDto input)
    {
        var features = input.Features is { Length: > 0 }
            ? string.Join(", ", input.Features)
            : "well-maintained, good location, close to amenities";

        var prompt =
            $"Write a short, attractive property listing description in Roman Urdu + English for a " +
            $"{input.AreaMarla?.ToString() ?? "?"} marla {input.Type.ToString().ToLowerInvariant()} " +
            $"in {input.Locality}, {input.City}. Bedrooms: {input.Bedrooms?.ToString() ?? "not specified"}, " +
            $"bathrooms: {input.Bathrooms?.ToString() ?? "not specified"}. Price: {FormatPkr(input.Price)}. " +
            $"Features: {features}. Keep it under 80 words. Do not invent facts not given here.";

        var result = await CallModelAsync(
            "You are a Pakistani real estate copywriter. Write clear, honest listing descriptions.",
            prompt);

        return result ?? LocalDescription(input, features);
    }

    public async Task<PriceEstimateResponseDto> EstimatePriceAsync(PriceEstimateDto input)
    {
        var all = await _properties.GetForAiContextAsync(100);

        var comparables = all
            .Where(p => p.Type.Equals(input.Type, StringComparison.OrdinalIgnoreCase))
            .Where(p => p.ListingType == "Sale")
            .ToList();

        var local = comparables
            .Where(p => p.City.Equals(input.City, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var sameLocality = local
            .Where(p => p.Locality.Equals(input.Locality, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var pool = sameLocality.Count >= 2 ? sameLocality : local.Count >= 2 ? local : comparables;

        decimal? perMarla = null;
        var withArea = pool.Where(p => p.AreaMarla is > 0).ToList();
        if (withArea.Count > 0)
            perMarla = withArea.Average(p => p.Price / p.AreaMarla!.Value);

        decimal estimate;
        string basis;

        if (perMarla.HasValue && input.AreaMarla is > 0)
        {
            estimate = perMarla.Value * input.AreaMarla.Value;
            basis = $"Based on {pool.Count} similar {input.Type.ToLowerInvariant()} listings" +
                    (sameLocality.Count >= 2 ? $" in {input.Locality}" : local.Count >= 2 ? $" in {input.City}" : " nationwide");
        }
        else if (pool.Count > 0)
        {
            estimate = pool.Average(p => p.Price);
            basis = $"Average of {pool.Count} similar listings";
        }
        else
        {
            estimate = input.Type.ToLowerInvariant() switch
            {
                "plot" => 3_000_000m,
                "apartment" => 6_000_000m,
                "shop" => 8_000_000m,
                _ => 12_000_000m
            };
            if (input.AreaMarla is > 0) estimate *= input.AreaMarla.Value / 5m;
            basis = "Market baseline (no close comparables available yet)";
        }

        estimate = Math.Round(estimate / 50_000m, MidpointRounding.AwayFromZero) * 50_000m;

        return new PriceEstimateResponseDto(
            estimate,
            Math.Round(estimate * 0.9m / 50_000m, MidpointRounding.AwayFromZero) * 50_000m,
            Math.Round(estimate * 1.1m / 50_000m, MidpointRounding.AwayFromZero) * 50_000m,
            basis);
    }

    // ---------- Phase 1: Listing Copilot ----------

    public async Task<GeneratedListingDto> GenerateListingAsync(GenerateListingInputDto input)
    {
        var notes = (input.Notes ?? string.Empty).Trim();
        var fallbackCity = string.IsNullOrWhiteSpace(input.City) ? "Lahore" : input.City!;
        var fallbackType = string.IsNullOrWhiteSpace(input.Type) ? "House" : input.Type!;
        var fallbackListing = string.IsNullOrWhiteSpace(input.ListingType) ? "Sale" : input.ListingType!;

        if (!string.IsNullOrWhiteSpace(_apiKey) && (notes.Length > 0 || input.ImageUrls is { Length: > 0 }))
        {
            var images = await BuildImageDataUrlsAsync(input.ImageUrls);
            var prompt =
                "Seller ne ye details di hain (Roman Urdu/English):\n" +
                (string.IsNullOrWhiteSpace(notes) ? "(no written notes)" : notes) + "\n\n" +
                $"Default city: {fallbackCity}. Default type: {fallbackType}. Default listing: {fallbackListing}.\n" +
                (images.Count > 0 ? $"{images.Count} property photos bhi di gayi hain — unse signals lo.\n\n" : "\n") +
                "Return ONLY a JSON object with these exact keys: title, description, type, listingType, city, " +
                "locality, bedrooms, bathrooms, areaMarla, price, highlights. " +
                "type must be one of House, Plot, Apartment, Shop, Office, Villa, Farmhouse. " +
                "listingType must be Sale or Rent. Numbers must be plain numbers (no commas, no units). " +
                "Write title and description in friendly Roman Urdu. Do not invent a price if no clue exists — use 0.";

            var raw = await CallModelAsync(
                "You are a Pakistani real estate listing assistant. You extract facts from seller notes/photos " +
                "and write honest, attractive Roman Urdu listings. Never fabricate legal details.",
                prompt,
                images);

            var parsed = ParseGeneratedListing(raw, fallbackCity, fallbackType, fallbackListing);
            if (parsed is not null) return parsed with { Mode = "ai" };
        }

        return await LocalGenerateListingAsync(notes, fallbackCity, fallbackType, fallbackListing);
    }

    public async Task<SellPriceResponseDto> SellPriceAsync(SellPriceInputDto input)
    {
        var all = await _properties.GetForAiContextAsync(100);
        var comparables = all
            .Where(p => p.Type.Equals(input.Type, StringComparison.OrdinalIgnoreCase))
            .Where(p => p.ListingType == "Sale")
            .ToList();

        var local = comparables
            .Where(p => p.City.Equals(input.City, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var sameLocality = local
            .Where(p => p.Locality.Equals(input.Locality, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var pool = sameLocality.Count >= 3 ? sameLocality : local.Count >= 3 ? local : comparables;
        var withArea = pool.Where(p => p.AreaMarla is > 0).ToList();

        decimal basePrice;
        string basis;
        if (withArea.Count > 0 && input.AreaMarla is > 0)
        {
            var perMarla = Median(withArea.Select(p => p.Price / p.AreaMarla!.Value));
            basePrice = perMarla * input.AreaMarla.Value;
            basis = $"Median price-per-marla of {pool.Count} similar listings" +
                    (sameLocality.Count >= 3 ? $" in {input.Locality}" : local.Count >= 3 ? $" in {input.City}" : " nationwide");
        }
        else if (pool.Count > 0)
        {
            basePrice = Median(pool.Select(p => p.Price));
            basis = $"Median of {pool.Count} similar listings";
        }
        else
        {
            basePrice = input.Type.ToLowerInvariant() switch
            {
                "plot" => 3_000_000m,
                "apartment" => 6_000_000m,
                _ => 12_000_000m
            };
            if (input.AreaMarla is > 0) basePrice *= input.AreaMarla.Value / 5m;
            basis = "Market baseline (abhi close comparables nahi hain)";
        }

        var multiplier = input.TargetDays switch
        {
            <= 15 => 0.93m, // price a bit sharper to move quickly
            <= 30 => 0.98m,
            _ => 1.03m      // patient seller can ask a little above market
        };

        var recommended = Round50K(basePrice * multiplier);
        var low = Round50K(recommended * 0.95m);
        var high = Round50K(recommended * 1.05m);
        var confidence = pool.Count >= 8 ? "High" : pool.Count >= 4 ? "Medium" : "Low";

        var advice = input.TargetDays switch
        {
            <= 15 => "Jaldi sell ke liye price market se thoda neeche rakhein. Photos aur verified documents turant add karein — isse buyers ka trust badhta hai.",
            <= 30 => "Ye price market ke mutabik hai. 5 achi photos + Registry verification ke saath listing 30 din mein sell hone ke strong imkaan hai.",
            _ => "Aap thoda upar price try kar sakte hain, lekin response slow ho sakta hai. Agar 2 hafte mein calls na aayein to price 3–5% kam karein."
        };

        return new SellPriceResponseDto(recommended, low, high, input.TargetDays, confidence, basis, advice);
    }

    public async Task<ListingQualityResponseDto> QualityAsync(ListingQualityInputDto input)
    {
        var checks = new List<QualityCheckDto>();
        var title = input.Title ?? string.Empty;
        var description = input.Description ?? string.Empty;
        var locality = input.Locality ?? string.Empty;
        var city = input.City ?? string.Empty;
        var images = (input.ImageUrls ?? Array.Empty<string>()).Length;

        checks.Add(Check("Strong title", 15,
            title.Length >= 15 && (title.Contains(city, StringComparison.OrdinalIgnoreCase) || locality.Length > 0),
            "City/type ke saath 15+ characters ka clear title likhein."));

        checks.Add(Check("Description", 20,
            description.Length >= 120,
            "Kam se kam 120 characters likhein — nearby amenities, condition aur selling point batayein."));

        checks.Add(Check("Photos", 20,
            images >= 5,
            images >= 3
                ? "Front, bedrooms, kitchen, bathroom aur balcony ki 2 aur photos add karein."
                : "Kam se kam 5 clear photos upload karein — listings zyada click leti hain."));

        decimal estimate = 0;
        try
        {
            var priceInfo = await EstimatePriceAsync(new PriceEstimateDto(
                input.Type, city, locality, input.AreaMarla, input.Bedrooms, input.Bathrooms));
            estimate = priceInfo.EstimatedPrice;
        }
        catch { /* quality score should still work without comparables */ }

        var priceOk = input.Price > 0 && (estimate <= 0 || input.Price >= estimate * 0.7m && input.Price <= estimate * 1.3m);
        checks.Add(Check("Realistic price", 15, priceOk,
            estimate > 0
                ? $"Similar listings ke hisaab se {FormatPkr(estimate)} ke aas-paas price rakhein."
                : "Price add karein jo similar listings ke mutabik ho."));

        checks.Add(Check("Location details", 10,
            locality.Length > 0 && !string.IsNullOrWhiteSpace(input.Address),
            "Locality aur full address dono bharain — buyers map par confidence ke saath dekhte hain."));

        checks.Add(Check("Key facts", 10,
            input.Bedrooms > 0 && input.Bathrooms > 0 && input.AreaMarla > 0,
            "Bedrooms, bathrooms aur marla area fill karein."));

        checks.Add(Check("Verified documents", 10,
            input.VerifiedDocuments >= 3,
            "Registry, Mutation aur NOC verify karke Verified badge paayein."));

        var score = checks.Sum(c => c.Points);
        var label = score >= 85 ? "Excellent" : score >= 70 ? "Good" : score >= 50 ? "Needs work" : "Poor";
        return new ListingQualityResponseDto(score, label, checks);
    }

    public async Task<WhatsAppReplyDto> WhatsAppReplyAsync(WhatsAppReplyInputDto input)
    {
        var listings = await _properties.GetForAiContextAsync(24);
        var ids = FindMatchingIds(input.Message, listings);
        if (input.PropertyId.HasValue && !ids.Contains(input.PropertyId.Value))
            ids = new[] { input.PropertyId.Value }.Concat(ids).ToArray();

        var selected = listings.Where(p => ids.Contains(p.Id)).Take(4).ToList();
        var context = selected.Count == 0 ? "No matching listings right now." : BuildContext(selected);

        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            var reply = await CallModelAsync(
                "You are the WhatsApp assistant for a Pakistani real estate agency. Reply in friendly Roman Urdu, " +
                "max 80 words. Use ONLY the listings provided. Never invent facts. Ask one clear next step (site visit / budget / phone).",
                $"Buyer ne WhatsApp par likha: \"{input.Message}\"\n\nAvailable listings:\n{context}");

            if (!string.IsNullOrWhiteSpace(reply))
                return new WhatsAppReplyDto(reply.Trim(), ids.Take(3).ToArray());
        }

        var localReply = selected.Count == 0
            ? "Assalam-o-Alaikum! Main RealEstate AI agent hoon. Aap city, budget aur property type bata dein — main aapko 3 best options bhej dunga."
            : "Assalam-o-Alaikum! 👋 Main RealEstate AI agent hoon. Aapki requirement ke mutabik ye options hain:\n" +
              string.Join("\n", selected.Take(3).Select(p =>
                  $"• {p.Title} — {FormatPkr(p.Price, p.ListingType)} ({p.Locality}, {p.City})")) +
              "\n\nKoi pasand aaye to site visit ka time bata dein — main arrange kar dunga.";

        return new WhatsAppReplyDto(localReply, ids.Take(3).ToArray());
    }

    // ---------- private helpers ----------

    private static QualityCheckDto Check(string name, int points, bool done, string tip)
        => new(name, done ? points : 0, points, done, done ? "Done ✓" : tip);

    private static decimal Median(IEnumerable<decimal> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        if (sorted.Count == 0) return 0;
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2m;
    }

    private static decimal Round50K(decimal value)
        => Math.Round(value / 50_000m, MidpointRounding.AwayFromZero) * 50_000m;

    private static decimal ParseAmount(string text)
    {
        foreach (Match m in Regex.Matches(text,
                     @"(\d+(?:\.\d+)?)\s*(crore|cor|lakh|lac|million|k|m)?\b",
                     RegexOptions.IgnoreCase))
        {
            if (!decimal.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                continue;

            var unit = m.Groups[2].Value.ToLowerInvariant();
            var amount = unit switch
            {
                "crore" or "cor" => value * 10_000_000m,
                "lakh" or "lac" => value * 100_000m,
                "k" => value * 1_000m,
                "m" or "million" => value * 1_000_000m,
                _ => value
            };

            if (amount >= 100_000) return amount;
        }
        return 0;
    }

    private async Task<List<string>> BuildImageDataUrlsAsync(string[]? urls)
    {
        var result = new List<string>();
        var root = !string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? _env.WebRootPath
            : Path.Combine(_env.ContentRootPath, "wwwroot");

        foreach (var url in (urls ?? Array.Empty<string>()).Take(4))
        {
            try
            {
                byte[] bytes;
                string mime;

                if (url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                {
                    var path = Path.Combine(root, "uploads", Path.GetFileName(url));
                    if (!File.Exists(path)) continue;
                    bytes = await File.ReadAllBytesAsync(path);
                    mime = Path.GetExtension(path).ToLowerInvariant() switch
                    {
                        ".png" => "image/png",
                        ".webp" => "image/webp",
                        ".gif" => "image/gif",
                        _ => "image/jpeg"
                    };
                }
                else if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    using var response = await _http.GetAsync(uri);
                    if (!response.IsSuccessStatusCode) continue;
                    bytes = await response.Content.ReadAsByteArrayAsync();
                    mime = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                }
                else continue;

                if (bytes.Length == 0 || bytes.Length > 3 * 1024 * 1024) continue;
                result.Add($"data:{mime};base64,{Convert.ToBase64String(bytes)}");
            }
            catch
            {
                // Skip images we cannot read — the text fallback still works.
            }
        }

        return result;
    }

    private static GeneratedListingDto? ParseGeneratedListing(
        string? raw, string fallbackCity, string fallbackType, string fallbackListing)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        try
        {
            var json = raw.Trim();
            if (json.StartsWith("```"))
            {
                var first = json.IndexOf('\n');
                var last = json.LastIndexOf("```", StringComparison.Ordinal);
                if (first >= 0 && last > first) json = json[(first + 1)..last].Trim();
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string Str(string key, string fallback) =>
                root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
                    ? v.GetString() ?? fallback
                    : fallback;

            int? Int(string key)
            {
                if (!root.TryGetProperty(key, out var v)) return null;
                if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n)) return n;
                if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var s)) return s;
                return null;
            }

            decimal Dec(string key)
            {
                if (!root.TryGetProperty(key, out var v)) return 0;
                if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var n)) return n;
                if (v.ValueKind == JsonValueKind.String &&
                    decimal.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s)) return s;
                return 0;
            }

            var title = Str("title", string.Empty).Trim();
            if (title.Length == 0) return null;

            var typeRaw = Str("type", fallbackType);
            var type = string.IsNullOrWhiteSpace(typeRaw) ? (ParseType(title) ?? fallbackType) : typeRaw.Trim();

            var listingRaw = Str("listingType", fallbackListing);
            var listingType = listingRaw.Equals("Rent", StringComparison.OrdinalIgnoreCase) ? "Rent" : "Sale";

            var highlights = root.TryGetProperty("highlights", out var h) && h.ValueKind == JsonValueKind.Array
                ? h.EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString() ?? string.Empty)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Take(6)
                    .ToArray()
                : Array.Empty<string>();

            return new GeneratedListingDto(
                title,
                Str("description", string.Empty),
                type,
                listingType,
                Str("city", fallbackCity),
                Str("locality", string.Empty),
                Int("bedrooms"),
                Int("bathrooms"),
                Int("areaMarla"),
                Dec("price"),
                highlights,
                "ai");
        }
        catch
        {
            return null;
        }
    }

    private async Task<GeneratedListingDto> LocalGenerateListingAsync(
        string notes, string city, string type, string listingType)
    {
        var text = notes.ToLowerInvariant();
        var bedrooms = Regex.Match(text, @"(\d+)\s*(?:bed|bedroom|br)\b") is { Success: true } b
            ? int.Parse(b.Groups[1].Value) : (int?)null;
        var bathrooms = Regex.Match(text, @"(\d+)\s*(?:bath|washroom|bathroom)\b") is { Success: true } ba
            ? int.Parse(ba.Groups[1].Value) : (int?)null;
        var marla = Regex.Match(text, @"(\d+(?:\.\d+)?)\s*(?:marla|marley)\b") is { Success: true } m
            ? (int?)decimal.ToInt32(decimal.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)) : null;
        var price = ParseAmount(text);

        var parsedType = ParseType(text) ?? type;
        var parsedCity = KnownCities.FirstOrDefault(c => text.Contains(c.ToLowerInvariant())) ?? city;
        var parsedListing = text.Contains("rent") || text.Contains("kiraya") || text.Contains("lease")
            ? "Rent" : listingType;

        var localities = new[]
        {
            "Bahria Town", "DHA", "Gulberg", "Johar Town", "F-11", "G-11", "Gulberg Greens",
            "Model Town", "Society", "Phase 6", "Phase 5", "Block A", "Block B"
        };
        var locality = localities.FirstOrDefault(l => text.Contains(l.ToLowerInvariant())) ?? string.Empty;

        var title = $"{bedrooms?.ToString() ?? parsedType} {parsedType} in {(locality.Length > 0 ? locality + ", " : "")}{parsedCity}";
        var highlights = new List<string>();
        if (bedrooms.HasValue) highlights.Add($"{bedrooms} bedrooms");
        if (bathrooms.HasValue) highlights.Add($"{bathrooms} bathrooms");
        if (marla.HasValue) highlights.Add($"{marla} marla");
        if (locality.Length > 0) highlights.Add($"Prime location in {locality}");

        var description =
            $"{title} — {parsedCity} mein ek achi {parsedType.ToLowerInvariant()} listing. " +
            (highlights.Count > 0 ? string.Join(", ", highlights) + ". " : string.Empty) +
            "Approximate location, bright rooms aur comfortable living ke saath. " +
            "Site visit ke liye message karein — similar options bhi bhej sakte hain.";

        if (price <= 0)
        {
            var estimate = await EstimatePriceAsync(new PriceEstimateDto(
                parsedType,
                parsedCity,
                locality,
                marla,
                bedrooms,
                bathrooms));
            price = estimate.EstimatedPrice;
        }

        return new GeneratedListingDto(
            title,
            description,
            parsedType,
            parsedListing,
            parsedCity,
            locality,
            bedrooms,
            bathrooms,
            marla,
            price,
            highlights.ToArray(),
            "local");
    }

    private static string? ParseType(string text)
    {
        var lower = text.ToLowerInvariant();
        if (lower.Contains("apartment") || lower.Contains("flat")) return "Apartment";
        if (lower.Contains("plot") || lower.Contains("land") || lower.Contains("jaagir")) return "Plot";
        if (lower.Contains("shop") || lower.Contains("store") || lower.Contains("dukaan")) return "Shop";
        if (lower.Contains("office")) return "Office";
        if (lower.Contains("villa")) return "Villa";
        if (lower.Contains("farm")) return "Farmhouse";
        if (lower.Contains("house") || lower.Contains("home") || lower.Contains("ghar")) return "House";
        return null;
    }

    private AiChatResponseDto LocalChat(string message, IReadOnlyList<PropertyDto> all)
    {
        var text = message.ToLowerInvariant();
        var active = all.Where(p => p.Status == "Active").ToList();

        var city = KnownCities.FirstOrDefault(c => text.Contains(c.ToLowerInvariant()));
        var type = new[]
        {
            "apartment", "house", "plot", "shop", "office", "villa", "farmhouse"
        }.FirstOrDefault(t => text.Contains(t));
        var wantsRent = Regex.IsMatch(text, @"\b(rent|rental|rental|kiraya|lease)\b");
        var wantsSale = Regex.IsMatch(text, @"\b(buy|purchase|sale|khareed|purchase)\b");
        var bedMatch = Regex.Match(text, @"(\d+)\s*(?:bed|bedroom|br)\b");
        int? beds = bedMatch.Success && int.TryParse(bedMatch.Groups[1].Value, out var b) ? b : null;

        decimal? budget = null;
        foreach (Match m in Regex.Matches(text, @"(\d+(?:\.\d+)?)\s*(crore|cor|lakh|lac|million|k|m)?\b"))
        {
            if (!decimal.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                continue;

            var unit = m.Groups[2].Value.ToLowerInvariant();
            var amount = unit switch
            {
                "crore" or "cor" => value * 10_000_000m,
                "lakh" or "lac" => value * 100_000m,
                "k" => value * 1_000m,
                "m" or "million" => value * 1_000_000m,
                _ => value
            };

            if (amount >= 10_000)
                budget = budget.HasValue ? Math.Min(budget.Value, amount) : amount;
        }

        var matches = active.AsEnumerable();

        if (!string.IsNullOrEmpty(city))
            matches = matches.Where(p =>
                p.City.Contains(city, StringComparison.OrdinalIgnoreCase) ||
                p.Locality.Contains(city, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(type))
            matches = matches.Where(p => p.Type.Contains(type, StringComparison.OrdinalIgnoreCase));

        var listingType = wantsRent ? "Rent" : wantsSale ? "Sale" : null;
        if (listingType is not null)
            matches = matches.Where(p => p.ListingType == listingType);

        if (beds.HasValue)
            matches = matches.Where(p => p.Bedrooms >= beds.Value);

        if (budget.HasValue)
            matches = matches.Where(p => p.Price <= budget.Value * 1.1m);

        var top = matches
            .OrderByDescending(p => p.IsFeatured)
            .ThenBy(p => Math.Abs(p.Price - (budget ?? p.Price)))
            .Take(3)
            .ToList();

        var description = top.Count switch
        {
            0 => "Mujhe is wakt aapki requirement se match koi property nahi mili. " +
                 "Aap city, budget (e.g. '80 lakh') aur type (house/plot/apartment) batayein, " +
                 "ya rent ke liye 'rent' likh dein — main dobara search kar dunga.",
            _ => $"Maine aapki baat samajh li. {top.Count} achi properties mili hain:\n" +
                 string.Join("\n", top.Select(p =>
                     $"• #{p.Id} — {p.Title} ({p.Locality}, {p.City}), {FormatPkr(p.Price, p.ListingType)}" +
                     (p.Bedrooms.HasValue ? $", {p.Bedrooms} beds" : "") +
                     (p.AreaMarla.HasValue ? $", {p.AreaMarla} marla" : ""))) +
                 "\n\nKisi ko dekhna hai? To neeche diye gaye controls se inquiry bhej dein ya 'schedule visit' kehlein."
        };

        return new AiChatResponseDto(description, "local", top.Select(p => p.Id).ToArray());
    }

    private int[] FindMatchingIds(string message, IReadOnlyList<PropertyDto> listings)
    {
        return LocalChat(message, listings).RecommendedPropertyIds;
    }

    private static string BuildContext(IReadOnlyList<PropertyDto> listings)
    {
        if (listings.Count == 0) return "No properties are currently listed.";

        return string.Join("\n", listings.Select(p =>
            $"#{p.Id} | {p.Title} | {p.Type} for {p.ListingType} | {FormatPkr(p.Price, p.ListingType)} | " +
            $"{p.Locality}, {p.City} | {p.AreaMarla?.ToString() ?? "?"} marla | {p.Bedrooms?.ToString() ?? "?"} beds"));
    }

    private static string LocalDescription(DescribeDto input, string features)
    {
        var parts = new List<string>
        {
            $"{input.Title} — {input.Locality}, {input.City} mein ek {input.AreaMarla?.ToString() ?? " suitable"} marla {input.Type.ToLowerInvariant()}"
        };

        if (input.Bedrooms.HasValue) parts.Add($"{input.Bedrooms} bedrooms");
        if (input.Bathrooms.HasValue) parts.Add($"{input.Bathrooms} bathrooms");
        parts.Add(features);

        return string.Join(". ", parts) + $". Ask for a viewing today! Price: {FormatPkr(input.Price)}.";
    }

    public static string FormatPkr(decimal amount, string listingType = "Sale")
    {
        var suffix = listingType.Equals("Rent", StringComparison.OrdinalIgnoreCase) ? "/month" : "";
        if (amount >= 10_000_000m) return $"{(amount / 10_000_000m):0.##} crore PKR{suffix}";
        if (amount >= 100_000m) return $"{(amount / 100_000m):0.##} lakh PKR{suffix}";
        return $"{amount:N0} PKR{suffix}";
    }

    private async Task<string?> CallModelAsync(
        string system,
        string user,
        IReadOnlyList<string>? imageDataUrls = null)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return null;

        try
        {
            // Vision models accept a content array with text + image_url parts.
            object userContent;
            if (imageDataUrls is { Count: > 0 })
            {
                var parts = new List<object> { new { type = "text", text = user } };
                foreach (var url in imageDataUrls)
                    parts.Add(new { type = "image_url", image_url = new { url } });
                userContent = parts;
            }
            else
            {
                userContent = user;
            }

            object systemContent = system;
            var messages = new object[]
            {
                new { role = "system", content = systemContent },
                new { role = "user", content = userContent }
            };

            var body = new
            {
                model = _model,
                messages,
                temperature = 0.4
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        catch
        {
            return null;
        }
    }
}
