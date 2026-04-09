using AzureMarketplaceSandbox.Data;
using AzureMarketplaceSandbox.Domain.Enums;
using AzureMarketplaceSandbox.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AzureMarketplaceSandbox.Services;

public class OperationService(MarketplaceDbContext db)
{
    public async Task<List<Operation>> ListPendingAsync(Guid subscriptionId)
    {
        return await db.Operations
            .Where(o => o.SubscriptionId == subscriptionId && o.Status == OperationStatus.InProgress)
            .OrderByDescending(o => o.TimeStamp)
            .ToListAsync();
    }

    public async Task<Operation?> GetAsync(Guid subscriptionId, Guid operationId)
    {
        return await db.Operations
            .FirstOrDefaultAsync(o => o.OperationId == operationId && o.SubscriptionId == subscriptionId);
    }

    /// <summary>
    /// Updates the operation status and applies the change to the subscription on success.
    /// </summary>
    public async Task<(bool Found, bool Updated, string? Error)> UpdateStatusAsync(
        Guid subscriptionId, Guid operationId, string status)
    {
        var operation = await db.Operations
            .FirstOrDefaultAsync(o => o.OperationId == operationId && o.SubscriptionId == subscriptionId);

        if (operation is null)
            return (false, false, null);

        if (operation.Status != OperationStatus.InProgress)
            return (true, false, "Operation is no longer in progress. A newer update may have already been applied.");

        var subscription = await db.Subscriptions
            .Include(s => s.Term)
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

        if (subscription is null)
            return (false, false, null);

        if (status.Equals("Success", StringComparison.OrdinalIgnoreCase))
        {
            operation.Status = OperationStatus.Succeeded;
            ApplyOperationToSubscription(operation, subscription);
        }
        else if (status.Equals("Failure", StringComparison.OrdinalIgnoreCase))
        {
            operation.Status = OperationStatus.Failed;
        }
        else
        {
            return (true, false, "Status must be 'Success' or 'Failure'.");
        }

        await db.SaveChangesAsync();
        return (true, true, null);
    }

    private static void ApplyOperationToSubscription(Operation operation, Subscription subscription)
    {
        switch (operation.Action)
        {
            case OperationAction.ChangePlan:
                subscription.PlanId = operation.PlanId;
                break;

            case OperationAction.ChangeQuantity:
                subscription.Quantity = operation.Quantity;
                break;

            case OperationAction.Unsubscribe:
                subscription.SaasSubscriptionStatus = SaasSubscriptionStatus.Unsubscribed;
                break;

            case OperationAction.Reinstate:
                subscription.SaasSubscriptionStatus = SaasSubscriptionStatus.Subscribed;
                break;

            case OperationAction.Suspend:
                subscription.SaasSubscriptionStatus = SaasSubscriptionStatus.Suspended;
                break;

            case OperationAction.Renew:
                subscription.Term.StartDate = DateTime.UtcNow;
                subscription.Term.EndDate = subscription.Term.CalculateEndDate();
                break;
        }
    }
}
