namespace RealEstateAI.Api.Services;

/// <summary>
/// Standard document checklist for Pakistani property transactions.
/// Used to seed a per-listing verification checklist.
/// </summary>
public static class DocumentDefaults
{
    public static IReadOnlyList<string> For(string type, string listingType)
    {
        var items = new List<string>
        {
            "Property title documents",
            "CNIC copies (both parties)",
            "Recent photographs of property"
        };

        if (listingType.Equals("Rent", StringComparison.OrdinalIgnoreCase))
        {
            items.AddRange(new[]
            {
                "Rent agreement / lease deed",
                "Owner CNIC & ownership proof",
                "Tenant CNIC copies",
                "NOC from society / landlord",
                "Rent receipt & security deposit proof",
                "Utility bills and meter readings",
                "Possession / handover note",
                "Police verification (if required)"
            });
        }
        else
        {
            items.AddRange(new[]
            {
                "Registry / Sale deed (Fard)",
                "Mutation (D Khata / Gawana)",
                "NOC from society / developer",
                "Agreement to sell",
                "Token money receipt",
                "Allotment / possession letter",
                "Khassra / site plan",
                "Power of attorney (if applicable)",
                "Society transfer / membership letter",
                "Utility connections (electricity, water, gas)"
            });
        }

        var houseLike = new[] { "House", "Villa", "Farmhouse" };
        var landLike = new[] { "Plot", "Farmhouse" };
        if (houseLike.Contains(type, StringComparer.OrdinalIgnoreCase))
            items.Add("Building plan approval");

        if (landLike.Contains(type, StringComparer.OrdinalIgnoreCase))
            items.Add("Land measurement / delineation report");

        items.Add("Encumbrance / lien check");
        return items;
    }
}
