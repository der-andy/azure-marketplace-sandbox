using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AzureMarketplaceSandbox.Domain.Enums;
using AzureMarketplaceSandbox.Domain.Models;
using AzureMarketplaceSandbox.Tests.Infrastructure;

namespace AzureMarketplaceSandbox.Tests.Api;

public class FulfillmentOperationsTests
{
    [Fact]
    public async Task OperationLifecycle_ChangePlan_FullFlow()
    {
        using var factory = new SandboxWebApplicationFactory();
        var subId = Guid.NewGuid();
        await factory.SeedAsync(async db =>
        {
            var offer = new Offer { OfferId = "offer1", PublisherId = "pub1", DisplayName = "O1" };
            offer.Plans.Add(new Plan { PlanId = "silver", DisplayName = "Silver" });
            offer.Plans.Add(new Plan { PlanId = "gold", DisplayName = "Gold" });
            db.Offers.Add(offer);
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

        var client = factory.CreateAuthenticatedClient();

        // Step 1: PATCH to change plan → 202
        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/saas/subscriptions/{subId}?api-version=2018-08-31",
            new { planId = "gold" });
        Assert.Equal(HttpStatusCode.Accepted, patchResponse.StatusCode);

        // Step 2: Extract operation ID from Operation-Location header
        var opLocation = patchResponse.Headers.GetValues("Operation-Location").First();
        var opUrl = new Uri(opLocation).PathAndQuery;

        // Step 3: GET operation → InProgress
        var opResponse = await client.GetFromJsonAsync<JsonElement>(opUrl);
        Assert.Equal("InProgress", opResponse.GetProperty("status").GetString());
        Assert.Equal("ChangePlan", opResponse.GetProperty("action").GetString());

        // Step 4: List pending operations
        var listResponse = await client.GetFromJsonAsync<JsonElement>(
            $"/api/saas/subscriptions/{subId}/operations?api-version=2018-08-31");
        var ops = listResponse.GetProperty("operations");
        Assert.True(ops.GetArrayLength() >= 1);

        // Step 5: PATCH operation with Success
        var updateResponse = await client.PatchAsJsonAsync(opUrl, new { status = "Success" });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Step 6: Verify subscription plan changed
        var sub = await client.GetFromJsonAsync<JsonElement>(
            $"/api/saas/subscriptions/{subId}?api-version=2018-08-31");
        Assert.Equal("gold", sub.GetProperty("planId").GetString());
    }

    [Fact]
    public async Task PatchOperation_WithFailure_DoesNotApplyChange()
    {
        using var factory = new SandboxWebApplicationFactory();
        var subId = Guid.NewGuid();
        await factory.SeedAsync(async db =>
        {
            var offer = new Offer { OfferId = "offer1", PublisherId = "pub1", DisplayName = "O1" };
            offer.Plans.Add(new Plan { PlanId = "silver", DisplayName = "Silver" });
            offer.Plans.Add(new Plan { PlanId = "gold", DisplayName = "Gold" });
            db.Offers.Add(offer);
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

        var client = factory.CreateAuthenticatedClient();

        // Change plan
        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/saas/subscriptions/{subId}?api-version=2018-08-31",
            new { planId = "gold" });
        var opUrl = new Uri(patchResponse.Headers.GetValues("Operation-Location").First()).PathAndQuery;

        // Reject the change
        await client.PatchAsJsonAsync(opUrl, new { status = "Failure" });

        // Verify plan did NOT change
        var sub = await client.GetFromJsonAsync<JsonElement>(
            $"/api/saas/subscriptions/{subId}?api-version=2018-08-31");
        Assert.Equal("silver", sub.GetProperty("planId").GetString());
    }
}
