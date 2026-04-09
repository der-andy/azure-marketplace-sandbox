using AzureMarketplaceSandbox.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AzureMarketplaceSandbox.Data;

public class MarketplaceDbContext(DbContextOptions<MarketplaceDbContext> options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Migrations are generated with SQLite but may run against SqlServer.
        // The provider difference causes a false-positive pending-changes warning.
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<MeteringDimension> MeteringDimensions => Set<MeteringDimension>();
    public DbSet<PlanMeteringDimension> PlanMeteringDimensions => Set<PlanMeteringDimension>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Operation> Operations => Set<Operation>();
    public DbSet<UsageEvent> UsageEvents => Set<UsageEvent>();
    public DbSet<MarketplaceToken> MarketplaceTokens => Set<MarketplaceToken>();
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MarketplaceToken>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).HasMaxLength(256);
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.HasIndex(e => e.OfferId).IsUnique();
            entity.Property(e => e.OfferId).HasMaxLength(128);
            entity.HasMany(e => e.Plans)
                .WithOne(p => p.Offer)
                .HasForeignKey(p => p.OfferId)
                .HasPrincipalKey(e => e.OfferId);
            entity.HasMany(e => e.MeteringDimensions)
                .WithOne(d => d.Offer)
                .HasForeignKey(d => d.OfferId)
                .HasPrincipalKey(e => e.OfferId);
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.Property(e => e.OfferId).HasMaxLength(128);
            entity.Ignore(e => e.PlanComponents);
        });

        modelBuilder.Entity<MeteringDimension>(entity =>
        {
            entity.Property(e => e.OfferId).HasMaxLength(128);
            entity.Ignore(e => e.Currency);
        });

        modelBuilder.Entity<PlanMeteringDimension>(entity =>
        {
            entity.HasKey(e => new { e.PlanId, e.MeteringDimensionId });
            entity.HasOne(e => e.Plan)
                .WithMany(p => p.PlanMeteringDimensions)
                .HasForeignKey(e => e.PlanId);
            entity.HasOne(e => e.MeteringDimension)
                .WithMany(d => d.PlanMeteringDimensions)
                .HasForeignKey(e => e.MeteringDimensionId);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasOne(e => e.Beneficiary).WithMany().HasForeignKey("BeneficiaryId");
            entity.HasOne(e => e.Purchaser).WithMany().HasForeignKey("PurchaserId");
            entity.HasOne(e => e.Term).WithMany().HasForeignKey("TermId");
            entity.HasMany(e => e.Operations)
                .WithOne(o => o.Subscription)
                .HasForeignKey(o => o.SubscriptionId);
            entity.Property(e => e.SaasSubscriptionStatus).HasConversion<string>();
            entity.Ignore(e => e.AllowedCustomerOperations);
        });

        modelBuilder.Entity<Operation>(entity =>
        {
            entity.Property(e => e.Action).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<UsageEvent>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<WebhookDeliveryLog>(entity =>
        {
            entity.Property(e => e.Action).HasConversion<string>();
        });
    }
}
