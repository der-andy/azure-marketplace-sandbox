using AzureMarketplaceSandbox.Domain.Models;
using AzureMarketplaceSandbox.Services;
using Microsoft.EntityFrameworkCore;

namespace AzureMarketplaceSandbox.Data;

public class MarketplaceDbContext(
    DbContextOptions<MarketplaceDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    private readonly ITenantContext _tenantContext = tenantContext;

    public DbSet<Tenant> Tenants => Set<Tenant>();
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
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EntraObjectId).IsUnique();
            entity.HasIndex(e => e.ApiBearerToken).IsUnique();
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.UserPrincipalName).HasMaxLength(256);
            entity.Property(e => e.ApiBearerToken).HasMaxLength(128);
            entity.Property(e => e.PublisherId).HasMaxLength(128);
            entity.Property(e => e.WebhookUrl).HasMaxLength(2048);
            entity.Property(e => e.LandingPageUrl).HasMaxLength(2048);
        });

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
            entity.HasIndex(e => new { e.TenantId, e.Token }).IsUnique();
            entity.Property(e => e.Token).HasMaxLength(256);
            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.OfferId }).IsUnique();
            entity.Property(e => e.OfferId).HasMaxLength(128);
            entity.HasMany(e => e.Plans)
                .WithOne(p => p.Offer);
            entity.HasMany(e => e.MeteringDimensions)
                .WithOne(d => d.Offer);
            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Ignore(e => e.PlanComponents);
            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<MeteringDimension>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PricePerUnit).HasPrecision(18, 4);
            entity.Ignore(e => e.Currency);
            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
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
            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Beneficiary).WithMany().HasForeignKey("BeneficiaryId").OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Purchaser).WithMany().HasForeignKey("PurchaserId").OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Term).WithMany().HasForeignKey("TermId");
            entity.Property(e => e.SaasSubscriptionStatus).HasConversion<string>();
            entity.Ignore(e => e.AllowedCustomerOperations);
            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<Operation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<UsageEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<WebhookDeliveryLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasConversion<string>();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
        });
    }
}
