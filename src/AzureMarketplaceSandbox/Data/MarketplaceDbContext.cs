using AzureMarketplaceSandbox.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AzureMarketplaceSandbox.Data;

public class MarketplaceDbContext(DbContextOptions<MarketplaceDbContext> options) : DbContext(options)
{
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
        modelBuilder.Entity<AadInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<SubscriptionTerm>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<MarketplaceToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).HasMaxLength(256);
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OfferId).IsUnique();
            entity.Property(e => e.OfferId).HasMaxLength(128);
            entity.HasMany(e => e.Plans)
                .WithOne(p => p.Offer);
            entity.HasMany(e => e.MeteringDimensions)
                .WithOne(d => d.Offer);
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Ignore(e => e.PlanComponents);
        });

        modelBuilder.Entity<MeteringDimension>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PricePerUnit).HasPrecision(18, 4);
            entity.Ignore(e => e.Currency);
        });

        modelBuilder.Entity<PlanMeteringDimension>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IncludedQuantity).HasPrecision(18, 4);
            entity.HasOne(e => e.Plan)
                .WithMany(p => p.PlanMeteringDimensions)
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.MeteringDimension)
                .WithMany(d => d.PlanMeteringDimensions)
                .HasForeignKey(e => e.MeteringDimensionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Beneficiary).WithMany().HasForeignKey("BeneficiaryId").OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Purchaser).WithMany().HasForeignKey("PurchaserId").OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Term).WithMany().HasForeignKey("TermId");
            entity.Property(e => e.SaasSubscriptionStatus).HasConversion<string>();
            entity.Ignore(e => e.AllowedCustomerOperations);
        });

        modelBuilder.Entity<Operation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<UsageEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<WebhookDeliveryLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasConversion<string>();
        });
    }
}
