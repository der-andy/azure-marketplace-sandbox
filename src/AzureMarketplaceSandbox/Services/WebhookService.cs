using System.Text.Json;
using AzureMarketplaceSandbox.Data;
using AzureMarketplaceSandbox.Domain.Enums;
using AzureMarketplaceSandbox.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AzureMarketplaceSandbox.Services;

public class WebhookService(
    IHttpClientFactory httpClientFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<WebhookService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task SendWebhookAsync(int tenantId, Guid subscriptionId, OperationAction action,
        string? newPlanId = null, int? newQuantity = null, Guid? operationId = null)
    {
        // New scope for the fire-and-forget delivery. Seed ITenantContext so the Global Query
        // Filter can locate the caller's subscription and tag the delivery log correctly.
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var tenantCtx = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantCtx.Set(tenantId, Guid.Empty);

        var webhookUrl = await db.Tenants.IgnoreQueryFilters()
            .Where(t => t.Id == tenantId)
            .Select(t => t.WebhookUrl)
            .FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            logger.LogWarning("Webhook URL is not configured for tenant {TenantId}. Skipping webhook for {Action} on subscription {SubscriptionId}.",
                tenantId, action, subscriptionId);
            return;
        }

        var subscription = await db.Subscriptions
            .Include(s => s.Beneficiary)
            .Include(s => s.Purchaser)
            .Include(s => s.Term)
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

        if (subscription is null)
        {
            logger.LogWarning("Subscription {SubscriptionId} not found. Skipping webhook.", subscriptionId);
            return;
        }

        var payload = new WebhookPayload
        {
            Id = operationId ?? Guid.NewGuid(),
            ActivityId = Guid.NewGuid(),
            PublisherId = subscription.PublisherId,
            OfferId = subscription.OfferId,
            PlanId = newPlanId ?? subscription.PlanId,
            Quantity = newQuantity ?? subscription.Quantity,
            SubscriptionId = subscriptionId,
            TimeStamp = DateTime.UtcNow,
            Action = action,
            Status = IsNotifyOnly(action) ? OperationStatus.Succeeded : OperationStatus.InProgress,
            Subscription = subscription,
            PurchaseToken = null
        };

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        var deliveryLog = new WebhookDeliveryLog
        {
            SubscriptionId = subscriptionId,
            Action = action,
            PayloadJson = payloadJson,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            var client = httpClientFactory.CreateClient("Webhook");
            var response = await client.PostAsync(webhookUrl,
                new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json"));

            deliveryLog.ResponseStatusCode = (int)response.StatusCode;
            deliveryLog.ResponseBody = await response.Content.ReadAsStringAsync();
            deliveryLog.Success = response.IsSuccessStatusCode;

            logger.LogInformation("Webhook {Action} for subscription {SubscriptionId} delivered. Status: {StatusCode}",
                action, subscriptionId, response.StatusCode);
        }
        catch (Exception ex)
        {
            deliveryLog.Success = false;
            deliveryLog.ErrorMessage = ex.Message;
            logger.LogError(ex, "Webhook {Action} delivery failed for subscription {SubscriptionId}.", action, subscriptionId);
        }

        db.WebhookDeliveryLogs.Add(deliveryLog);
        await db.SaveChangesAsync();
    }

    private static bool IsNotifyOnly(OperationAction action) =>
        action is OperationAction.Renew or OperationAction.Suspend or OperationAction.Unsubscribe;
}
