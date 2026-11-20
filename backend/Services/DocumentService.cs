using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface IDocumentService
{
    Task<List<DocumentItemDto>> GetAsync(int propertyId);
    Task<DocumentItemDto?> AddAsync(int propertyId, CreateDocumentDto input);
    Task<DocumentItemDto?> UpdateAsync(int id, UpdateDocumentDto input);
    Task<bool> DeleteAsync(int id);
    Task<OcrScanResultDto> ScanAsync(int propertyId, string text);
}

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _db;

    public DocumentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<DocumentItemDto>> GetAsync(int propertyId)
    {
        return await _db.DocumentChecklist
            .AsNoTracking()
            .Where(d => d.PropertyListingId == propertyId)
            .OrderBy(d => d.SortOrder)
            .Select(d => ToDto(d))
            .ToListAsync();
    }

    public async Task<DocumentItemDto?> AddAsync(int propertyId, CreateDocumentDto input)
    {
        if (!await _db.Properties.AnyAsync(p => p.Id == propertyId)) return null;

        var maxOrder = await _db.DocumentChecklist
            .Where(d => d.PropertyListingId == propertyId)
            .Select(d => (int?)d.SortOrder)
            .MaxAsync() ?? -1;

        var item = new DocumentChecklistItem
        {
            PropertyListingId = propertyId,
            Name = input.Name,
            Notes = input.Notes,
            SortOrder = maxOrder + 1
        };

        _db.DocumentChecklist.Add(item);
        await _db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<DocumentItemDto?> UpdateAsync(int id, UpdateDocumentDto input)
    {
        var item = await _db.DocumentChecklist.FindAsync(id);
        if (item is null) return null;

        item.Notes = input.Notes;
        item.IsVerified = input.IsVerified;
        item.VerifiedAt = input.IsVerified ? DateTime.UtcNow : null;

        await _db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.DocumentChecklist.FindAsync(id);
        if (item is null) return false;
        _db.DocumentChecklist.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Matches text extracted from a scanned document image against the
    /// property's checklist and auto-verifies the items it recognizes.
    /// </summary>
    public async Task<OcrScanResultDto> ScanAsync(int propertyId, string text)
    {
        var items = await _db.DocumentChecklist
            .Where(d => d.PropertyListingId == propertyId)
            .OrderBy(d => d.SortOrder)
            .ToListAsync();

        var normalized = Normalize(text);
        var matchedNames = new List<string>();
        var newlyVerified = 0;

        foreach (var item in items)
        {
            var keywords = KeywordsFor(item.Name);
            if (keywords.Length == 0) continue;
            if (!keywords.Any(k => normalized.Contains(Normalize(k)))) continue;

            matchedNames.Add(item.Name);
            if (!item.IsVerified)
            {
                item.IsVerified = true;
                item.VerifiedAt = DateTime.UtcNow;
                newlyVerified++;
            }
        }

        if (newlyVerified > 0) await _db.SaveChangesAsync();

        return new OcrScanResultDto(
            matchedNames.Count,
            newlyVerified,
            matchedNames,
            items.Select(ToDto).ToList());
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var chars = value.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : ' ')
            .ToArray();
        return string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string[] KeywordsFor(string name)
    {
        var key = name.ToLowerInvariant().Trim();
        if (OcrKeywords.TryGetValue(key, out var words)) return words;

        // Fallback: use the distinctive tail of a custom item name.
        var tokens = Normalize(name).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 3 && !StopWords.Contains(t))
            .ToArray();
        return tokens.TakeLast(2).ToArray();
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "documents", "document", "copies", "copy", "proof", "both", "parties",
        "recent", "from", "society", "landlord", "applicable", "required",
        "connections", "electricity", "if", "and", "the", "for", "with", "letter"
    };

    private static readonly Dictionary<string, string[]> OcrKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Property title documents"] = new[] { "title deed", "property title", "title document", "registry deed", "sale deed", "fard" },
        ["CNIC copies (both parties)"] = new[] { "cnic", "national identity card", "identity card" },
        ["Recent photographs of property"] = Array.Empty<string>(),
        ["Rent agreement / lease deed"] = new[] { "rent agreement", "lease deed", "lease agreement", "rent deed" },
        ["Owner CNIC & ownership proof"] = new[] { "owner cnic", "ownership proof", "owner identity" },
        ["Tenant CNIC copies"] = new[] { "tenant cnic", "tenant identity" },
        ["NOC from society / landlord"] = new[] { "noc", "no objection" },
        ["Rent receipt & security deposit proof"] = new[] { "rent receipt", "security deposit", "deposit receipt" },
        ["Utility bills and meter readings"] = new[] { "utility bill", "electricity bill", "meter reading", "gas bill", "water bill" },
        ["Possession / handover note"] = new[] { "possession", "handover", "hand over" },
        ["Police verification (if required)"] = new[] { "police verification", "police clearance" },
        ["Registry / Sale deed (Fard)"] = new[] { "registry", "sale deed", "fard", "registered deed" },
        ["Mutation (D Khata / Gawana)"] = new[] { "mutation", "d khata", "gawana", "khata" },
        ["Agreement to sell"] = new[] { "agreement to sell", "sale agreement", "bainama" },
        ["Token money receipt"] = new[] { "token money", "token receipt", "earnest money" },
        ["Allotment / possession letter"] = new[] { "allotment", "possession letter" },
        ["Khassra / site plan"] = new[] { "khassra", "site plan", "plot plan" },
        ["Power of attorney (if applicable)"] = new[] { "power of attorney", "poa", "vakalat" },
        ["Society transfer / membership letter"] = new[] { "society transfer", "membership letter", "transfer letter" },
        ["Utility connections (electricity, water, gas)"] = new[] { "utility connection", "water connection", "gas connection" },
        ["Building plan approval"] = new[] { "building plan", "plan approval", "approved plan" },
        ["Land measurement / delineation report"] = new[] { "land measurement", "delineation", "survey report", "measurement report" },
        ["Encumbrance / lien check"] = new[] { "encumbrance", "lien", "clear title" }
    };

    private static DocumentItemDto ToDto(DocumentChecklistItem d) => new(
        d.Id,
        d.PropertyListingId,
        d.Name,
        d.Notes,
        d.IsVerified,
        d.VerifiedAt,
        d.SortOrder);
}
