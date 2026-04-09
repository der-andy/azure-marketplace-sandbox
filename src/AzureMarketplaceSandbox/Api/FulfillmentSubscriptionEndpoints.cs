using AzureMarketplaceSandbox.Configuration;
using AzureMarketplaceSandbox.Data;
using AzureMarketplaceSandbox.Domain.Models;
using AzureMarketplaceSandbox.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AzureMarketplaceSandbox.Api;

public static class FulfillmentSubscriptionEndpoints
{
    public static void MapFulfillmentSubscriptionApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/saas/subscriptions")
            .RequireAuthorization();

        // POST /resolve
        group.MapPost("/resolve", async (
            HttpContext context,
            TokenService tokenService,
            SubscriptionService subscriptionService,
            MarketplaceDbContext db) =>
        {
            if (!context.Request.Headers.TryGetValue("x-ms-marketplace-token", out var tokenHeader) ||
                string.IsNullOrWhiteSpace(tokenHeader))
            {
                return Results.BadRequest(new { message = "x-ms-marketplace-token header is required.", code = "BadArgument" });
            }

            var decoded = Uri.UnescapeDataString(tokenHeader.ToString());
            var marketplaceToken = await tokenService.ResolveTokenAsync(decoded);
            if (marketplaceToken is null)
            {
                return Results.BadRequest(new { message = "Token is missing, malformed, invalid, or expired.", code = "BadArgument" });
            }

            var subscription = await subscriptionService.GetAsync(marketplaceToken.SubscriptionId);
            if (subscription is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                id = subscription.SubscriptionId,
                subscriptionName = subscription.Name,
                offerId = subscription.OfferId,
                planId = subscription.PlanId,
                quantity = subscription.Quantity,
                subscription
            });
        });

        // POST /{subscriptionId}/activate
        group.MapPost("/{subscriptionId:guid}/activate", async (
            Guid subscriptionId,
            HttpContext context,
            SubscriptionService subscriptionService) =>
        {
            ActivateSubscriptionRequest? body = null;
            try { body = await context.Request.ReadFromJsonAsync<ActivateSubscriptionRequest>(); }
            catch { /* empty or invalid body */ }
            if (body is null || string.IsNullOrWhiteSpace(body.PlanId))
            {
                return Results.BadRequest(new { message = "Request body with 'planId' is required.", code = "BadArgument" });
            }

            var (success, error) = await subscriptionService.ActivateAsync(subscriptionId, body.PlanId, body.Quantity);
            if (!success)
            {
                return Results.BadRequest(new { message = error, code = "BadArgument" });
            }

            return Results.Ok();
        });

        // GET / (list all subscriptions)
        group.MapGet("/", async (
            string? continuationToken,
            SubscriptionService subscriptionService) =>
        {
            var (items, nextLink) = await subscriptionService.ListAsync(continuationToken);

            return Results.Ok(new
            {
                subscriptions = items,
                @nextLink = nextLink
            });
        });

        // GET /{subscriptionId}
        group.MapGet("/{subscriptionId:guid}", async (
            Guid subscriptionId,
            SubscriptionService subscriptionService) =>
        {
            var subscription = await subscriptionService.GetAsync(subscriptionId);
            if (subscription is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(subscription);
        });

        // GET /{subscriptionId}/listAvailablePlans
        group.MapGet("/{subscriptionId:guid}/listAvailablePlans", async (
            Guid subscriptionId,
            SubscriptionService subscriptionService,
            MarketplaceDbContext db) =>
        {
            var subscription = await db.Subscriptions.FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);
            if (subscription is null)
            {
                return Results.NotFound();
            }

            var plans = await subscriptionService.ListAvailablePlansAsync(subscriptionId);
            return Results.Ok(new { plans });
        });

        // PATCH /{subscriptionId} (change plan or quantity)
        group.MapPatch("/{subscriptionId:guid}", async (
            Guid subscriptionId,
            HttpContext context,
            SubscriptionService subscriptionService,
            WebhookService webhookService,
            IOptions<SandboxOptions> sandboxOptions) =>
        {
            var body = await context.Request.ReadFromJsonAsync<PatchSubscriptionRequest>();
            if (body is null)
            {
                return Results.BadRequest(new { message = "Request body is required.", code = "BadArgument" });
            }

            // Cannot change both at once
            if (body.PlanId is not null && body.Quantity is not null)
            {
                return Results.BadRequest(new { message = "Either plan or quantity can be changed at one time, not both.", code = "BadArgument" });
            }

            Operation? operation = null;

            if (body.PlanId is not null)
            {
                operation = await subscriptionService.ChangePlanAsync(subscriptionId, body.PlanId);
            }
            else if (body.Quantity is not null)
            {
                operation = await subscriptionService.ChangeQuantityAsync(subscriptionId, body.Quantity.Value);
            }

            if (operation is null)
            {
                return Results.BadRequest(new { message = "The change could not be applied. Verify the subscription exists, is Subscribed, and the new value is valid.", code = "BadArgument" });
            }

            var baseUrl = sandboxOptions.Value.BaseUrl.TrimEnd('/');
            var operationLocation = $"{baseUrl}/api/saas/subscriptions/{subscriptionId}/operations/{operation.OperationId}?api-version=2018-08-31";
            context.Response.Headers["Operation-Location"] = operationLocation;

            // Fire webhook asynchronously
            var action = body.PlanId is not null
                ? Domain.Enums.OperationAction.ChangePlan
                : Domain.Enums.OperationAction.ChangeQuantity;
            _ = webhookService.SendWebhookAsync(subscriptionId, action,
                newPlanId: body.PlanId, newQuantity: body.Quantity, operationId: operation.OperationId);

            return Results.Accepted(operationLocation);
        });

        // DELETE /{subscriptionId} (unsubscribe)
        group.MapDelete("/{subscriptionId:guid}", async (
            Guid subscriptionId,
            HttpContext context,
            SubscriptionService subscriptionService,
            WebhookService webhookService,
            MarketplaceDbContext db,
            IOptions<SandboxOptions> sandboxOptions) =>
        {
            var subscription = await subscriptionService.GetAsync(subscriptionId);
            if (subscription is null)
            {
                return Results.NotFound();
            }

            if (subscription.SaasSubscriptionStatus == Domain.Enums.SaasSubscriptionStatus.Unsubscribed)
            {
                return Results.Ok();
            }

            var operation = await subscriptionService.UnsubscribeAsync(subscriptionId);
            if (operation is null)
            {
                return Results.BadRequest(new { message = "Subscription cannot be unsubscribed.", code = "BadArgument" });
            }

            var baseUrl = sandboxOptions.Value.BaseUrl.TrimEnd('/');
            var operationLocation = $"{baseUrl}/api/saas/subscriptions/{subscriptionId}/operations/{operation.OperationId}?api-version=2018-08-31";
            context.Response.Headers["Operation-Location"] = operationLocation;

            _ = webhookService.SendWebhookAsync(subscriptionId, Domain.Enums.OperationAction.Unsubscribe,
                operationId: operation.OperationId);

            return Results.Accepted(operationLocation);
        });
    }

    private record ActivateSubscriptionRequest
    {
        public string? PlanId { get; init; }
        public int? Quantity { get; init; }
    }

    private record PatchSubscriptionRequest
    {
        public string? PlanId { get; init; }
        public int? Quantity { get; init; }
    }
}
