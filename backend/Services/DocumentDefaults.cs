using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

/// <summary>
/// Standard document checklist for Pakistani property transactions.
/// Used to seed a per-listing verification checklist.
/// </summary>
public static class DocumentDefaults
{
    public static IReadOnlyList<string> For(PropertyType type, ListingType listingType)
    {
        var items = new List<string>
        {
            "Property title documents",
            "CNIC copies (both parties)",
            "Recent photographs of property"
        };

        if (listingType == ListingType.Rent)
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

        if (type is PropertyType.House or PropertyType.Villa or PropertyType.Farmhouse)
            items.Add("Building plan approval");

        if (type is PropertyType.Plot or PropertyType.Farmhouse)
            items.Add("Land measurement / delineation report");

        items.Add("Encumbrance / lien check");
        return items;
    }
}
