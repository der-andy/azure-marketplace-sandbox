using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AzureMarketplaceSandbox.Data;
using AzureMarketplaceSandbox.Domain.Enums;
using AzureMarketplaceSandbox.Domain.Models;
using AzureMarketplaceSandbox.Services;
using AzureMarketplaceSandbox.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AzureMarketplaceSandbox.Tests.Api;

public class MeteringTests
{
    private async Task<(SandboxWebApplicationFactory Factory, Guid SubId)> CreateFactoryWithSubscription()
    {
        var factory = new SandboxWebApplicationFactory();
        var subId = Guid.NewGuid();
        await factory.SeedAsync(async db =>
        {
            var dimension = new MeteringDimension
            {
                DimensionId = "api-calls",
                DisplayName = "API Calls",
                UnitOfMeasure = "calls",
                PricePerUnit = 0.01m
            };
            var offer = new Offer
            {
                OfferId = "offer1", PublisherId = "pub1", DisplayName = "O1", Currency = "USD",
                MeteringDimensions = [dimension]
            };
            var plan = new Plan { PlanId = "silver", DisplayName = "Silver" };
            offer.Plans.Add(plan);
            db.Offers.Add(offer);
            await db.SaveChangesAsync();
            db.PlanMeteringDimensions.Add(new PlanMeteringDimension
            {
                PlanId = plan.Id,
                MeteringDimensionId = dimension.Id
            });
            db.Subscriptions.Add(new Subscription
            {
                SubscriptionId = subId,
                Name = "Test",
                OfferId = "offer1",
                PublisherId = "pub1",
                PlanId = "silver",
                Quantity = 5,
                SaasSubscriptionStatus = SaasSubscriptionStatus.Subscribed,
                Beneficiary = new AadInfo { EmailId = "t@t.com" },
                Purchaser = new AadInfo { EmailId = "t@t.com" },
                Term = new SubscriptionTerm()
            });
            await db.SaveChangesAsync();
        });
        return (factory, subId);
    }

    [Fact]
    public async Task PostUsageEvent_Valid_ReturnsAccepted()
    {
        var (factory, subId) = await CreateFactoryWithSubscription();
        using var _ = factory;
        var client = factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/usageEvent?api-version=2018-08-31", new
        {
            resourceId = subId,
            quantity = 10.0,
            dimension = "api-calls",
            effectiveStartTime = DateTime.UtcNow.AddMinutes(-5),
            planId = "silver"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Accepted", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PostUsageEvent_Duplicate_DetectedByService()
    {
        // Test duplicate detection with separate DbContext instances per call (mimics scoped DI)
        var dbName = $"TestDb-Dup-{Guid.NewGuid()}";

        // Seed data
        var seedOptions = new DbContextOptionsBuilder<MarketplaceDbContext>()
            .UseInMemoryDatabase(dbName).Options;
        var subId = Guid.NewGuid();
        using (var seedDb = new MarketplaceDbContext(seedOptions))
        {
            await seedDb.Database.EnsureCreatedAsync();
            var dimension = new MeteringDimension
            {
                DimensionId = "api-calls", DisplayName = "API Calls",
                UnitOfMeasure = "calls", PricePerUnit = 0.01m
            };
            var offer = new Offer
            {
                OfferId = "offer1", PublisherId = "pub1", DisplayName = "O1", Currency = "USD",
                MeteringDimensions = [dimension]
            };
            var plan = new Plan { PlanId = "silver", DisplayName = "Silver" };
            offer.Plans.Add(plan);
            seedDb.Offers.Add(offer);
            await seedDb.SaveChangesAsync();
            seedDb.PlanMeteringDimensions.Add(new PlanMeteringDimension
            {
                PlanId = plan.Id,
                MeteringDimensionId = dimension.Id
            });
            seedDb.Subscriptions.Add(new Subscription
            {
                SubscriptionId = subId, Name = "Test", OfferId = "offer1", PublisherId = "pub1",
                PlanId = "silver", Quantity = 5,
                SaasSubscriptionStatus = SaasSubscriptionStatus.Subscribed,
                Beneficiary = new AadInfo { EmailId = "t@t.com" },
                Purchaser = new AadInfo { EmailId = "t@t.com" },
                Term = new SubscriptionTerm()
            });
            await seedDb.SaveChangesAsync();
        }

        var effectiveTime = DateTime.UtcNow.AddMinutes(-30);
        var request = new UsageEventRequest
        {
            ResourceId = subId,
            Quantity = 10.0m,
            Dimension = "api-calls",
            EffectiveStartTime = effectiveTime,
            PlanId = "silver"
        };

        // First call with fresh DbContext: Accepted
        using (var db1 = new MarketplaceDbContext(seedOptions))
        {
            var service1 = new MeteringService(db1);
            var first = await service1.PostUsageEventAsync(request);
            Assert.Equal(UsageEventStatus.Accepted, first.Status);
        }

        // Second call with fresh DbContext: Duplicate
        using (var db2 = new MarketplaceDbContext(seedOptions))
        {
            // Verify data is visible
            var eventCount = await db2.UsageEvents.CountAsync();
            Assert.True(eventCount > 0, $"Expected events in DB but found {eventCount}");

            var service2 = new MeteringService(db2);
            var second = await service2.PostUsageEventAsync(request);
            Assert.True(second.Status == UsageEventStatus.Duplicate,
                $"Expected Duplicate but got {second.Status}. DB events: {await db2.UsageEvents.CountAsync()}");
        }
    }

    [Fact]
    public async Task PostUsageEvent_InvalidDimension_Returns400()
    {
        var (factory, subId) = await CreateFactoryWithSubscription();
        using var _ = factory;
        var client = factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/usageEvent?api-version=2018-08-31", new
        {
            resourceId = subId,
            quantity = 10.0,
            dimension = "nonexistent-dimension",
            effectiveStartTime = DateTime.UtcNow.AddMinutes(-5),
            planId = "silver"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostUsageEvent_ZeroQuantity_Returns400()
    {
        var (factory, subId) = await CreateFactoryWithSubscription();
        using var _ = factory;
        var client = factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/usageEvent?api-version=2018-08-31", new
        {
            resourceId = subId,
            quantity = 0.0,
            dimension = "api-calls",
            effectiveStartTime = DateTime.UtcNow.AddMinutes(-5),
            planId = "silver"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BatchUsageEvent_MoreThan25_Returns400()
    {
        var (factory, subId) = await CreateFactoryWithSubscription();
        using var _ = factory;
        var client = factory.CreateAuthenticatedClient();

        var events = Enumerable.Range(0, 26).Select(i => new
        {
            resourceId = subId,
            quantity = 1.0,
            dimension = "api-calls",
            effectiveStartTime = DateTime.UtcNow.AddHours(-i),
            planId = "silver"
        }).ToList();

        var response = await client.PostAsJsonAsync("/api/batchUsageEvent?api-version=2018-08-31",
            new { request = events });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostUsageEvent_WithoutAuth_RejectsRequest()
    {
        using var factory = new SandboxWebApplicationFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.PostAsJsonAsync("/api/usageEvent?api-version=2018-08-31", new
        {
            resourceId = Guid.NewGuid(),
            quantity = 1.0,
            dimension = "dim",
            effectiveStartTime = DateTime.UtcNow,
            planId = "plan"
        });

        // Unauthenticated requests are rejected (401 or 400 depending on middleware order)
        Assert.False(response.IsSuccessStatusCode);
    }
}
