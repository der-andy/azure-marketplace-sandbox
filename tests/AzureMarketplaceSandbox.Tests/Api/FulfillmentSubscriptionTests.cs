using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AzureMarketplaceSandbox.Domain.Enums;
using AzureMarketplaceSandbox.Domain.Models;
using AzureMarketplaceSandbox.Tests.Infrastructure;

namespace AzureMarketplaceSandbox.Tests.Api;

public class FulfillmentSubscriptionTests : IClassFixture<SandboxWebApplicationFactory>
{
    private readonly SandboxWebApplicationFactory _factory;

    public FulfillmentSubscriptionTests(SandboxWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListSubscriptions_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/saas/subscriptions?api-version=2018-08-31");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListSubscriptions_WithAuth_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/saas/subscriptions?api-version=2018-08-31");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("subscriptions", out _));
    }

    [Fact]
    public async Task ListSubscriptions_WithoutApiVersion_Returns400()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/saas/subscriptions");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSubscription_NotFound_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/saas/subscriptions/{Guid.NewGuid()}?api-version=2018-08-31");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Activate_PendingSubscription_Returns200()
    {
        using var factory = new SandboxWebApplicationFactory();
        var subId = Guid.NewGuid();
        await factory.SeedAsync(async db =>
        {
            var offer = new Offer { OfferId = "test-offer", PublisherId = "pub1", DisplayName = "Test" };
            offer.Plans.Add(new Plan { PlanId = "plan1", DisplayName = "Plan 1", OfferId = "test-offer", IsPricePerSeat = true });
            db.Offers.Add(offer);
            db.Subscriptions.Add(new Subscription
            {
                Id = subId,
                Name = "Test Sub",
                OfferId = "test-offer",
                PublisherId = "pub1",
                PlanId = "plan1",
                Quantity = 1,
                SaasSubscriptionStatus = SaasSubscriptionStatus.PendingFulfillmentStart,
                Beneficiary = new AadInfo { EmailId = "test@test.com" },
                Purchaser = new AadInfo { EmailId = "test@test.com" },
                Term = new SubscriptionTerm { TermUnit = "P1M" }
            });
            await db.SaveChangesAsync();
        });

        var client = factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync(
            $"/api/saas/subscriptions/{subId}/activate?api-version=2018-08-31",
            new { planId = "plan1", quantity = 5 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify status changed and quantity updated
        var getResponse = await client.GetFromJsonAsync<JsonElement>(
            $"/api/saas/subscriptions/{subId}?api-version=2018-08-31");
        Assert.Equal("Subscribed", getResponse.GetProperty("saasSubscriptionStatus").GetString());
        Assert.Equal(5, getResponse.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task Activate_WithoutBody_Returns400()
    {
        using var factory = new SandboxWebApplicationFactory();
        var subId = Guid.NewGuid();
        await factory.SeedAsync(async db =>
        {
            db.Offers.Add(new Offer { OfferId = "o", PublisherId = "p", DisplayName = "O" });
            db.Subscriptions.Add(new Subscription
            {
                Id = subId,
                Name = "Test",
                OfferId = "o",
                PublisherId = "p",
                PlanId = "pl",
                SaasSubscriptionStatus = SaasSubscriptionStatus.PendingFulfillmentStart,
                Beneficiary = new AadInfo { EmailId = "t@t.com" },
                Purchaser = new AadInfo { EmailId = "t@t.com" },
                Term = new SubscriptionTerm()
            });
            await db.SaveChangesAsync();
        });

        var client = factory.CreateAuthenticatedClient();
        var response = await client.PostAsync(
            $"/api/saas/subscriptions/{subId}/activate?api-version=2018-08-31", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Activate_NonSeatBased_QuantityMustBeNull()
    {
        using var factory = new SandboxWebApplicationFactory();
        var subId = Guid.NewGuid();
        await factory.SeedAsync(async db =>
        {
            var offer = new Offer { OfferId = "test-offer", PublisherId = "pub1", DisplayName = "Test" };
            offer.Plans.Add(new Plan { PlanId = "free", DisplayName = "Free", OfferId = "test-offer", IsPricePerSeat = false });
            db.Offers.Add(offer);
            db.Subscriptions.Add(new Subscription
            {
                Id = subId,
                Name = "Test",
                OfferId = "test-offer",
                PublisherId = "pub1",
                PlanId = "free",
                SaasSubscriptionStatus = SaasSubscriptionStatus.PendingFulfillmentStart,
                Beneficiary = new AadInfo { EmailId = "t@t.com" },
                Purchaser = new AadInfo { EmailId = "t@t.com" },
                Term = new SubscriptionTerm()
            });
            await db.SaveChangesAsync();
        });

        var client = factory.CreateAuthenticatedClient();

        // With quantity on non-seat-based plan → 400
        var response = await client.PostAsJsonAsync(
            $"/api/saas/subscriptions/{subId}/activate?api-version=2018-08-31",
            new { planId = "free", quantity = 5 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Without quantity → 200
        var response2 = await client.PostAsJsonAsync(
            $"/api/saas/subscriptions/{subId}/activate?api-version=2018-08-31",
            new { planId = "free" });
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
    }

    [Fact]
    public async Task Activate_AlreadySubscribed_Returns400()
    {
        using var factory = new SandboxWebApplicationFactory();
        var subId = Guid.NewGuid();
        await factory.SeedAsync(async db =>
        {
            db.Subscriptions.Add(new Subscription
            {
                Id = subId,
                Name = "Test",
                OfferId = "o",
                PublisherId = "p",
                PlanId = "pl",
                SaasSubscriptionStatus = SaasSubscriptionStatus.Subscribed,
                Beneficiary = new AadInfo { EmailId = "t@t.com" },
                Purchaser = new AadInfo { EmailId = "t@t.com" },
                Term = new SubscriptionTerm()
            });
            await db.SaveChangesAsync();
        });

        var client = factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync(
            $"/api/saas/subscriptions/{subId}/activate?api-version=2018-08-31",
            new { planId = "pl" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePlan_Returns202WithOperationLocation()
    {
        using var factory = new SandboxWebApplicationFactory();
        var subId = Guid.NewGuid();
        await factory.SeedAsync(async db =>
        {
            var offer = new Offer { OfferId = "offer1", PublisherId = "pub1", DisplayName = "O1" };
            offer.Plans.Add(new Plan { PlanId = "silver", DisplayName = "Silver", OfferId = "offer1" });
            offer.Plans.Add(new Plan { PlanId = "gold", DisplayName = "Gold", OfferId = "offer1" });
            db.Offers.Add(offer);
            db.Subscriptions.Add(new Subscription
            {
                Id = subId,
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

        var client = factory.CreateAuthenticatedClient();
        var response = await client.PatchAsJsonAsync(
            $"/api/saas/subscriptions/{subId}?api-version=2018-08-31",
            new { planId = "gold" });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.True(response.Headers.Contains("Operation-Location"));
    }

    [Fact]
    public async Task Delete_SubscribedSubscription_Returns202()
    {
        using var factory = new SandboxWebApplicationFactory();
        var subId = Guid.NewGuid();
        await factory.SeedAsync(async db =>
        {
            db.Subscriptions.Add(new Subscription
            {
                Id = subId,
                Name = "Test",
                OfferId = "o",
                PublisherId = "p",
                PlanId = "pl",
                SaasSubscriptionStatus = SaasSubscriptionStatus.Subscribed,
                Beneficiary = new AadInfo { EmailId = "t@t.com" },
                Purchaser = new AadInfo { EmailId = "t@t.com" },
                Term = new SubscriptionTerm()
            });
            await db.SaveChangesAsync();
        });

        var client = factory.CreateAuthenticatedClient();
        var response = await client.DeleteAsync(
            $"/api/saas/subscriptions/{subId}?api-version=2018-08-31");
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.True(response.Headers.Contains("Operation-Location"));
    }

    [Fact]
    public async Task ResponseHeaders_ContainCorrelationIds()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/saas/subscriptions?api-version=2018-08-31");

        Assert.True(response.Headers.Contains("x-ms-requestid"));
        Assert.True(response.Headers.Contains("x-ms-correlationid"));
    }
}
