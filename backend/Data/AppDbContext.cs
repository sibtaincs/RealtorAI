using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<PropertyListing> Properties => Set<PropertyListing>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DocumentChecklistItem> DocumentChecklist => Set<DocumentChecklistItem>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<AgentProfile> AgentProfiles => Set<AgentProfile>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<ProjectContent> ProjectContents => Set<ProjectContent>();
    public DbSet<AgencyContent> AgencyContents => Set<AgencyContent>();
    public DbSet<SocietyContent> SocietyContents => Set<SocietyContent>();
    public DbSet<LandRecordContent> LandRecordContents => Set<LandRecordContent>();
    public DbSet<ArticleContent> ArticleContents => Set<ArticleContent>();
    public DbSet<AreaGuideContent> AreaGuideContents => Set<AreaGuideContent>();
    public DbSet<AreaTrendContent> AreaTrendContents => Set<AreaTrendContent>();
    public DbSet<PriceIndexContent> PriceIndexContents => Set<PriceIndexContent>();
    public DbSet<MasterData> MasterData => Set<MasterData>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PropertyListing>(entity =>
        {
            entity.HasOne(p => p.Agent)
                  .WithMany()
                  .HasForeignKey(p => p.AgentId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Title).HasMaxLength(200);
            entity.Property(p => p.Type).HasMaxLength(50);
            entity.Property(p => p.ListingType).HasMaxLength(50);

        builder.Entity<ProjectContent>(e => { e.Property(x => x.PriceFrom).HasColumnType("decimal(18,2)"); e.Property(x => x.PriceTo).HasColumnType("decimal(18,2)"); });
        builder.Entity<SocietyContent>(e => { e.Property(x => x.PriceFrom).HasColumnType("decimal(18,2)"); });
        builder.Entity<AreaGuideContent>(e => { e.Property(x => x.PricePerSqFt).HasColumnType("decimal(18,2)"); });
        builder.Entity<PriceIndexContent>(e => { e.Property(x => x.MedianPrice).HasColumnType("decimal(18,2)"); });
        });

        builder.Entity<Lead>(entity =>
        {
            entity.HasOne(l => l.PropertyListing)
                  .WithMany(p => p.Leads)
                  .HasForeignKey(l => l.PropertyListingId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<DocumentChecklistItem>(entity =>
        {
            entity.HasOne(d => d.PropertyListing)
                  .WithMany()
                  .HasForeignKey(d => d.PropertyListingId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(d => d.Name).HasMaxLength(200);
        });

        builder.Entity<SavedSearch>(entity =>
        {
            entity.HasOne(s => s.User)
                  .WithMany()
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(s => s.Name).HasMaxLength(120);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.HasIndex(n => new { n.UserId, n.IsRead });
        });

        builder.Entity<FollowUp>(entity =>
        {
            entity.HasOne(f => f.Lead)
                  .WithMany()
                  .HasForeignKey(f => f.LeadId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<NotificationPreference>().HasKey(p => p.UserId);

        builder.Entity<NotificationDelivery>(entity =>
        {
            entity.HasIndex(d => new { d.SavedSearchId, d.PropertyListingId, d.Channel }).IsUnique();
        });

        builder.Entity<AgentProfile>(entity =>
        {
            entity.HasKey(p => p.UserId);
            entity.HasOne(p => p.User)
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(p => p.Bio).HasMaxLength(1000);
            entity.Property(p => p.Phone).HasMaxLength(50);
        });

        builder.Entity<Review>(entity =>
        {
            entity.HasOne(r => r.PropertyListing)
                  .WithMany()
                  .HasForeignKey(r => r.PropertyListingId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Author)
                  .WithMany()
                  .HasForeignKey(r => r.AuthorId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(r => new { r.ModerationStatus, r.CreatedAt });
            entity.Property(r => r.Comment).HasMaxLength(2000);
        });

        builder.Entity<Report>(entity =>
        {
            entity.HasOne(r => r.PropertyListing)
                  .WithMany()
                  .HasForeignKey(r => r.PropertyListingId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Reporter)
                  .WithMany()
                  .HasForeignKey(r => r.ReporterId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(r => new { r.Status, r.CreatedAt });
            entity.Property(r => r.Reason).HasMaxLength(500);
        });

        builder.Entity<ContentItem>(entity =>
        {
            entity.HasIndex(c => c.Kind);
            entity.Property(c => c.Kind).HasMaxLength(50);
        });

        builder.Entity<MasterData>(entity =>
        {
            entity.HasIndex(m => new { m.Kind, m.Value });
            entity.Property(m => m.Kind).HasMaxLength(50);
            entity.Property(m => m.Value).HasMaxLength(100);
            entity.Property(m => m.Label).HasMaxLength(100);
        });
    }
}
