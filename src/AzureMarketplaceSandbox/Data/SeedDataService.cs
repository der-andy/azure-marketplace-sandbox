using AzureMarketplaceSandbox.Configuration;
using AzureMarketplaceSandbox.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AzureMarketplaceSandbox.Data;

public class SeedDataService(
    IServiceScopeFactory scopeFactory,
    IOptions<SeedDataOptions> seedOptions,
    IOptions<SandboxOptions> sandboxOptions,
    ILogger<SeedDataService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!seedOptions.Value.Enabled)
            return;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();

        if (await db.Offers.AnyAsync(cancellationToken))
            return;

        logger.LogInformation("Seeding default offer and plans...");

        var publisherId = sandboxOptions.Value.PublisherId;

        var apiCalls = new MeteringDimension
        {
            DimensionId = "api-calls",
            DisplayName = "API Calls",
            UnitOfMeasure = "calls",
            PricePerUnit = 0
        };
        var storageGb = new MeteringDimension
        {
            DimensionId = "storage-gb",
            DisplayName = "Storage",
            UnitOfMeasure = "GB",
            PricePerUnit = 0.10m
        };
        var computeHours = new MeteringDimension
        {
            DimensionId = "compute-hours",
            DisplayName = "Compute Hours",
            UnitOfMeasure = "hours",
            PricePerUnit = 0.50m
        };

        var freePlan = new Plan
        {
            PlanId = "free",
            DisplayName = "Free Tier",
            Description = "Free plan with limited features",
            IsPricePerSeat = false,
            MinQuantity = 0,
            MaxQuantity = 1,
            HasFreeTrials = false,
            Market = "US",
            BillingTermUnit = "P1M",
            SubscriptionTermUnit = "P1M"
        };
        var silverPlan = new Plan
        {
            PlanId = "silver",
            DisplayName = "Silver",
            Description = "Standard plan with core features",
            IsPricePerSeat = true,
            MinQuantity = 1,
            MaxQuantity = 50,
            HasFreeTrials = true,
            Market = "US",
            BillingTermUnit = "P1M",
            SubscriptionTermUnit = "P3M"
        };
        var goldPlan = new Plan
        {
            PlanId = "gold",
            DisplayName = "Gold",
            Description = "Premium plan with all features",
            IsPricePerSeat = true,
            MinQuantity = 1,
            MaxQuantity = 200,
            HasFreeTrials = false,
            Market = "US",
            BillingTermUnit = "P1Y",
            SubscriptionTermUnit = "P1Y"
        };

        var offer = new Offer
        {
            OfferId = "contoso-saas-offer",
            PublisherId = publisherId,
            DisplayName = "Contoso Cloud Solution",
            Currency = "USD",
            Plans = [freePlan, silverPlan, goldPlan],
            MeteringDimensions = [apiCalls, storageGb, computeHours]
        };

        db.Offers.Add(offer);
        await db.SaveChangesAsync(cancellationToken);

        db.PlanMeteringDimensions.AddRange(
            new PlanMeteringDimension { Plan = freePlan, MeteringDimension = apiCalls },
            new PlanMeteringDimension { Plan = silverPlan, MeteringDimension = apiCalls },
            new PlanMeteringDimension { Plan = silverPlan, MeteringDimension = storageGb },
            new PlanMeteringDimension { Plan = goldPlan, MeteringDimension = apiCalls },
            new PlanMeteringDimension { Plan = goldPlan, MeteringDimension = storageGb },
            new PlanMeteringDimension { Plan = goldPlan, MeteringDimension = computeHours }
        );
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded offer '{OfferId}' with {PlanCount} plans.", offer.OfferId, offer.Plans.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
