using AzureMarketplaceSandbox.Data;
using AzureMarketplaceSandbox.Domain.Enums;
using AzureMarketplaceSandbox.Domain.Models;
using AzureMarketplaceSandbox.Services;
using Microsoft.EntityFrameworkCore;

namespace AzureMarketplaceSandbox.Tests.Services;

public class SubscriptionServiceTests
{
    private static (MarketplaceDbContext Db, SubscriptionService Service) CreateService()
    {
        var options = new DbContextOptionsBuilder<MarketplaceDbContext>()
            .UseInMemoryDatabase($"TestDb-{Guid.NewGuid()}")
            .Options;
        var db = new MarketplaceDbContext(options);
        return (db, new SubscriptionService(db));
    }

    private static Subscription CreateSubscription(
        SaasSubscriptionStatus status = SaasSubscriptionStatus.PendingFulfillmentStart,
        string offerId = "offer1",
        string planId = "silver")
    {
        return new Subscription
        {
            SubscriptionId = Guid.NewGuid(),
            Name = "Test",
            OfferId = offerId,
            PublisherId = "pub1",
            PlanId = planId,
            Quantity = 5,
            SaasSubscriptionStatus = status,
            Beneficiary = new AadInfo { EmailId = "t@t.com" },
            Purchaser = new AadInfo { EmailId = "t@t.com" },
            Term = new SubscriptionTerm { TermUnit = "P1M" }
        };
    }

    [Fact]
    public async Task Activate_FromPending_Succeeds()
    {
        var (db, service) = CreateService();
        var offer = new Offer { OfferId = "offer1", PublisherId = "pub1", DisplayName = "O1" };
        offer.Plans.Add(new Plan { PlanId = "silver", DisplayName = "Silver", IsPricePerSeat = true });
        db.Offers.Add(offer);
        var sub = CreateSubscription(SaasSubscriptionStatus.PendingFulfillmentStart);
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();

        var (success, _) = await service.ActivateAsync(sub.SubscriptionId, "silver", 10);

        Assert.True(success);
        var updated = await db.Subscriptions.FirstOrDefaultAsync(s => s.SubscriptionId == sub.SubscriptionId);
        Assert.Equal(SaasSubscriptionStatus.Subscribed, updated!.SaasSubscriptionStatus);
        Assert.Equal(10, updated.Quantity);
    }

    [Theory]
    [InlineData(SaasSubscriptionStatus.Subscribed)]
    [InlineData(SaasSubscriptionStatus.Suspended)]
    [InlineData(SaasSubscriptionStatus.Unsubscribed)]
    public async Task Activate_FromNonPending_Fails(SaasSubscriptionStatus status)
    {
        var (db, service) = CreateService();
        var sub = CreateSubscription(status);
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();

        var (result, _) = await service.ActivateAsync(sub.SubscriptionId, sub.PlanId, null);
        Assert.False(result);
    }

    [Fact]
    public async Task ChangePlan_ToSamePlan_ReturnsNull()
    {
        var (db, service) = CreateService();
        var sub = CreateSubscription(SaasSubscriptionStatus.Subscribed);
        db.Subscriptions.Add(sub);
        var offer = new Offer { OfferId = "offer1", PublisherId = "pub1", DisplayName = "O1" };
        offer.Plans.Add(new Plan { PlanId = "silver", DisplayName = "Silver" });
        db.Offers.Add(offer);
        await db.SaveChangesAsync();

        var op = await service.ChangePlanAsync(sub.SubscriptionId, "silver");
        Assert.Null(op);
    }

    [Fact]
    public async Task ChangePlan_ToNewPlan_CreatesOperation()
    {
        var (db, service) = CreateService();
        var sub = CreateSubscription(SaasSubscriptionStatus.Subscribed);
        db.Subscriptions.Add(sub);
        var offer = new Offer { OfferId = "offer1", PublisherId = "pub1", DisplayName = "O1" };
        offer.Plans.Add(new Plan { PlanId = "silver", DisplayName = "Silver" });
        offer.Plans.Add(new Plan { PlanId = "gold", DisplayName = "Gold" });
        db.Offers.Add(offer);
        await db.SaveChangesAsync();

        var op = await service.ChangePlanAsync(sub.SubscriptionId, "gold");

        Assert.NotNull(op);
        Assert.Equal(OperationAction.ChangePlan, op.Action);
        Assert.Equal(OperationStatus.InProgress, op.Status);
        Assert.Equal("gold", op.PlanId);
    }

    [Fact]
    public async Task ChangeQuantity_ZeroOrNegative_ReturnsNull()
    {
        var (db, service) = CreateService();
        var sub = CreateSubscription(SaasSubscriptionStatus.Subscribed);
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();

        Assert.Null(await service.ChangeQuantityAsync(sub.SubscriptionId, 0));
        Assert.Null(await service.ChangeQuantityAsync(sub.SubscriptionId, -1));
    }

    [Fact]
    public async Task Unsubscribe_AlreadyUnsubscribed_ReturnsNull()
    {
        var (db, service) = CreateService();
        var sub = CreateSubscription(SaasSubscriptionStatus.Unsubscribed);
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();

        var op = await service.UnsubscribeAsync(sub.SubscriptionId);
        Assert.Null(op);
    }

    [Fact]
    public async Task Unsubscribe_Subscribed_CreatesOperation()
    {
        var (db, service) = CreateService();
        var sub = CreateSubscription(SaasSubscriptionStatus.Subscribed);
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();

        var op = await service.UnsubscribeAsync(sub.SubscriptionId);

        Assert.NotNull(op);
        Assert.Equal(OperationAction.Unsubscribe, op.Action);
    }

    [Fact]
    public async Task ListAsync_Pagination_Works()
    {
        var (db, service) = CreateService();
        for (int i = 0; i < 15; i++)
        {
            var sub = CreateSubscription();
            sub.Name = $"Sub {i}";
            sub.Created = DateTime.UtcNow.AddMinutes(i);
            db.Subscriptions.Add(sub);
        }
        await db.SaveChangesAsync();

        var (page1, nextLink1) = await service.ListAsync(null, 10);
        Assert.Equal(10, page1.Count);
        Assert.NotNull(nextLink1);

        // Extract continuation token
        var token = nextLink1.Split("continuationToken=")[1].Split("&")[0];
        var (page2, nextLink2) = await service.ListAsync(token, 10);
        Assert.Equal(5, page2.Count);
        Assert.Null(nextLink2);
    }
}
