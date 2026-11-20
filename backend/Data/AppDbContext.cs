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
    }
}
