using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Models;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles)
    {
        // Local SQL Server uses EF migrations so schema changes are versioned.
        // Hosted PostgreSQL keeps EnsureCreated (its database is created fresh on deploy).
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        foreach (var roleName in new[] { "Admin", "Agent" })
        {
            if (!await roles.RoleExistsAsync(roleName))
                await roles.CreateAsync(new IdentityRole(roleName));
        }

        var adminEmail = "admin@realestateai.pk";
        var admin = await users.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "AI Agent Admin",
                AgencyName = "RealEstate AI"
            };
            var created = await users.CreateAsync(admin, "Admin@123");
            if (created.Succeeded)
                await users.AddToRoleAsync(admin, "Admin");
        }

        var agentEmail = "agent@realestateai.pk";
        var agent = await users.FindByEmailAsync(agentEmail);
        if (agent is null)
        {
            agent = new ApplicationUser
            {
                UserName = agentEmail,
                Email = agentEmail,
                FullName = "Ayesha Khan",
                AgencyName = "RealEstate AI"
            };
            var created = await users.CreateAsync(agent, "Agent@123");
            if (created.Succeeded)
                await users.AddToRoleAsync(agent, "Agent");
        }

        if (admin is not null && agent is not null && !await db.AgentProfiles.AnyAsync())
        {
            db.AgentProfiles.AddRange(
                new AgentProfile
                {
                    UserId = admin.Id, Bio = "RealEstate AI lead agent. Helping buyers and sellers across Pakistan.",
                    Phone = "+92 300 1234567", Verified = true
                },
                new AgentProfile
                {
                    UserId = agent.Id, Bio = "Lahore-based property specialist with 8 years of experience.",
                    Phone = "+92 301 7654321", Verified = true, RatingAvg = 4.5, RatingCount = 2
                });
            await db.SaveChangesAsync();
        }

        if (!await db.Properties.AnyAsync())
        {
            var listings = BuildSeedListings();
            for (var i = 0; i < listings.Count; i++)
                listings[i].AgentId = i < 3 ? agent?.Id : admin?.Id;

            db.Properties.AddRange(listings);
            await db.SaveChangesAsync();

            foreach (var listing in listings)
            {
                var names = DocumentDefaults.For(listing.Type, listing.ListingType);
                for (var i = 0; i < names.Count; i++)
                {
                    db.DocumentChecklist.Add(new DocumentChecklistItem
                    {
                        PropertyListingId = listing.Id,
                        Name = names[i],
                        SortOrder = i
                    });
                }
            }
            await db.SaveChangesAsync();
        }

        // Existing databases were seeded before agents existed — give the demo agent a few listings.
        if (agent is not null && !await db.Properties.AnyAsync(p => p.AgentId == agent.Id))
        {
            var owned = await db.Properties.OrderBy(p => p.Id).Take(3).ToListAsync();
            foreach (var listing in owned) listing.AgentId = agent.Id;
            await db.SaveChangesAsync();
        }

        if (admin is not null && agent is not null && !await db.Reviews.AnyAsync())
        {
            var listing = await db.Properties.OrderBy(p => p.Id).FirstOrDefaultAsync();
            if (listing is not null)
            {
                db.Reviews.AddRange(
                    new Review
                    {
                        PropertyListingId = listing.Id, AuthorId = admin.Id, Rating = 5,
                        Comment = "Great communication and honest advice throughout the site visit.",
                        ModerationStatus = ReviewModerationStatus.Approved
                    },
                    new Review
                    {
                        PropertyListingId = listing.Id, AuthorId = agent.Id, Rating = 4,
                        Comment = "Very smooth process, documents were ready on time.",
                        ModerationStatus = ReviewModerationStatus.Approved
                    },
                    new Review
                    {
                        PropertyListingId = listing.Id, AuthorId = admin.Id, Rating = 3,
                        Comment = "Awaiting moderation seed.",
                        ModerationStatus = ReviewModerationStatus.Pending
                    });
                await db.SaveChangesAsync();
            }
        }

        if (agent is not null && !await db.Reports.AnyAsync())
        {
            var listing = await db.Properties.OrderBy(p => p.Id).FirstOrDefaultAsync();
            if (listing is not null)
            {
                db.Reports.Add(new Report
                {
                    PropertyListingId = listing.Id, ReporterId = agent.Id,
                    Reason = "Please verify the advertised area before approval."
                });
                await db.SaveChangesAsync();
            }
        }
    }

    private static List<PropertyListing> BuildSeedListings()
    {
        const string baseUrl = "https://images.unsplash.com/";
        string[] Images(params string[] ids) =>
            ids.Select(id => $"{baseUrl}{id}?w=900&q=80").ToArray();

        var home1 = Images("photo-1600596542815-ffad4c1539a9", "photo-1600607687939-ce8a6c25118c", "photo-1600566753190-17f0baa2a6c3");
        var home2 = Images("photo-1600585154340-be6161a56a0c", "photo-1600047509807-ba8f99d2cdde");
        var home3 = Images("photo-1568605114967-8130f3a36994", "photo-1570129477492-45c003edd2be");
        var flat = Images("photo-1600607687920-4e2a09cf159d", "photo-1600566753086-00f18fb6b3ea");
        var plot = Images("photo-1583608205776-bfd35f0d9f83", "photo-1500382017468-9049fed747ef");
        var shop = Images("photo-1600830178312-8540388f141f", "photo-1441986300917-64674bd600d8");

        return new List<PropertyListing>
        {
            New("Modern 10 Marla House in DHA Phase 6",
                "Spacious 10 marla house with 4 bedrooms, modern kitchen, lawn and car parking. Prime location near park and school.",
                22_500_000m, PropertyType.House, ListingType.Sale, "Lahore", "DHA Lahore Phase 6", "Block R, DHA Phase 6, Lahore",
                31.448, 74.380, 4, 5, 10, 2500, home1, true),

            New("5 Marla Family House in Bahria Town",
                "Well-maintained 5 marla house in a secure gated community. Near mosque, park and commercial area.",
                9_500_000m, PropertyType.House, ListingType.Sale, "Lahore", "Bahria Town Lahore", "Sector E, Bahria Town, Lahore",
                31.326, 74.190, 3, 4, 5, 1250, home2, true),

            New("1 Kanal Plot in DHA Phase 7",
                "Corner plot, ideal for a dream home. All utilities available. Located on a wide road.",
                18_000_000m, PropertyType.Plot, ListingType.Sale, "Lahore", "DHA Lahore Phase 7", "Block T, DHA Phase 7, Lahore",
                31.430, 74.370, null, null, 20, 5000, plot, false),

            New("Furnished 2 Bed Apartment in Gulberg",
                "Bright apartment in the heart of Gulberg. Close to shops, restaurants and main boulevard.",
                6_800_000m, PropertyType.Apartment, ListingType.Sale, "Lahore", "Gulberg III", "Gulberg III, Lahore",
                31.559, 74.341, 2, 2, 3, 750, flat, false),

            New("10 Marla Plot in Bahria Town Karachi",
                "Investment plot in a fast-developing area. Easy access to main highway and amenities.",
                4_200_000m, PropertyType.Plot, ListingType.Sale, "Karachi", "Bahria Town Karachi", "Precinct 12, Bahria Town, Karachi",
                25.196, 67.334, null, null, 10, 2500, plot, true),

            New("10 Marla House in DHA City Karachi",
                "Brand new 10 marla house with 4 bedrooms, drawing room and rooftop terrace.",
                8_900_000m, PropertyType.House, ListingType.Sale, "Karachi", "DHA City Karachi", "Sector 5, DHA City, Karachi",
                24.790, 67.330, 4, 4, 10, 2500, home3, false),

            New("2 Bed Apartment for Rent in Clifton",
                "Fully furnished apartment available for rent. Lift, parking and 24/7 security.",
                85_000m, PropertyType.Apartment, ListingType.Rent, "Karachi", "Clifton", "Clifton Block 4, Karachi",
                24.807, 67.030, 2, 2, null, 900, flat, false),

            New("Shop for Rent in Gulshan-e-Iqbal",
                "Ground floor shop on a busy main road. Suitable for retail, office or clinic.",
                120_000m, PropertyType.Shop, ListingType.Rent, "Karachi", "Gulshan-e-Iqbal", "Block 13-D, Gulshan-e-Iqbal, Karachi",
                24.893, 67.099, null, null, 4, 900, shop, false),

            New("Elegant 10 Marla House in DHA Islamabad",
                "Modern design with 4 bedrooms, basement and landscaped lawn. Close to schools and parks.",
                16_500_000m, PropertyType.House, ListingType.Sale, "Islamabad", "DHA Islamabad Phase 2", "Phase 2, DHA Islamabad",
                33.646, 73.070, 4, 5, 10, 2500, home1, true),

            New("5 Marla House in Bahria Town Rawalpindi",
                "Affordable family home in a secure society with all facilities nearby.",
                11_500_000m, PropertyType.House, ListingType.Sale, "Rawalpindi", "Bahria Town Phase 8", "Phase 8, Bahria Town, Rawalpindi",
                33.536, 73.045, 3, 4, 5, 1250, home2, false),

            New("8 Marla Plot in Gulberg Greens",
                "Farmhouse society plot with beautiful views. Ideal for a weekend home or investment.",
                7_600_000m, PropertyType.Plot, ListingType.Sale, "Islamabad", "Gulberg Greens", "Gulberg Greens, Islamabad",
                33.545, 73.180, null, null, 8, 2000, plot, false),

            New("2 Bed Apartment for Rent in Johar Town",
                "Clean and comfortable apartment near hospitals, metro station and shopping malls.",
                55_000m, PropertyType.Apartment, ListingType.Rent, "Lahore", "Johar Town", "Phase 1, Johar Town, Lahore",
                31.467, 74.273, 2, 2, 3, 750, flat, false),

            New("Luxury Villa in Bahria Town Karachi",
                "20 marla villa with 5 bedrooms, swimming pool and ample parking. Premium living.",
                15_000_000m, PropertyType.Villa, ListingType.Sale, "Karachi", "Bahria Town Karachi", "Precinct 8, Bahria Town, Karachi",
                25.200, 67.340, 5, 6, 20, 5000, home3, true),

            New("10 Marla House for Rent in F-11",
                "Well-maintained family house available for rent. Close to schools, markets and main roads.",
                140_000m, PropertyType.House, ListingType.Rent, "Islamabad", "F-11", "F-11/2, Islamabad",
                33.750, 73.050, 4, 4, 10, 2500, home2, false)
        };
    }

    private static PropertyListing New(
        string title,
        string description,
        decimal price,
        PropertyType type,
        ListingType listingType,
        string city,
        string locality,
        string address,
        double lat,
        double lng,
        int? bedrooms,
        int? bathrooms,
        int? marla,
        int? sqft,
        string[] images,
        bool featured) => new()
        {
            Title = title,
            Description = description,
            Price = price,
            Type = type,
            ListingType = listingType,
            City = city,
            Locality = locality,
            Address = address,
            Latitude = lat,
            Longitude = lng,
            Bedrooms = bedrooms,
            Bathrooms = bathrooms,
            AreaMarla = marla,
            AreaSqFt = sqft,
            ImageUrlsJson = JsonSerializer.Serialize(images),
            IsFeatured = featured
        };
}
