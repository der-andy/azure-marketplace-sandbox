using AzureMarketplaceSandbox.Data;
using AzureMarketplaceSandbox.Domain.Enums;
using AzureMarketplaceSandbox.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AzureMarketplaceSandbox.Services;

public class SubscriptionService(MarketplaceDbContext db)
{
    public async Task<Subscription?> GetAsync(Guid subscriptionId)
    {
        return await db.Subscriptions
            .Include(s => s.Beneficiary)
            .Include(s => s.Purchaser)
            .Include(s => s.Term)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);
    }

    public async Task<(List<Subscription> Items, string? NextLink)> ListAsync(string? continuationToken, int pageSize = 10)
    {
        int skip = 0;
        if (continuationToken is not null)
        {
            try { skip = int.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(continuationToken))); }
            catch { /* invalid token, start from 0 */ }
        }

        var total = await db.Subscriptions.CountAsync();
        var items = await db.Subscriptions
            .Include(s => s.Beneficiary)
            .Include(s => s.Purchaser)
            .Include(s => s.Term)
            .OrderBy(s => s.Created)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        string? nextLink = null;
        if (skip + pageSize < total)
        {
            var nextToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes((skip + pageSize).ToString()));
            nextLink = $"/api/saas/subscriptions?continuationToken={nextToken}&api-version=2018-08-31";
        }

        return (items, nextLink);
    }

    public async Task<List<Plan>> ListAvailablePlansAsync(Guid subscriptionId)
    {
        var subscription = await db.Subscriptions.FindAsync(subscriptionId);
        if (subscription is null)
            return [];

        return await db.Plans
            .Include(p => p.MeteringDimensions)
            .Where(p => p.OfferId == subscription.OfferId)
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> ActivateAsync(Guid subscriptionId, string planId, int? quantity)
    {
        var subscription = await GetAsync(subscriptionId);
        if (subscription is null)
            return (false, "Subscription not found.");

        if (subscription.SaasSubscriptionStatus != SaasSubscriptionStatus.PendingFulfillmentStart)
            return (false, "Subscription is not in PendingFulfillmentStart state.");

        if (planId != subscription.PlanId)
            return (false, $"planId '{planId}' does not match the subscription's plan '{subscription.PlanId}'.");

        // Check if the plan is seat-based — if so, quantity is required
        var plan = await db.Plans.FirstOrDefaultAsync(p => p.PlanId == planId && p.OfferId == subscription.OfferId);
        if (plan is not null && plan.IsPricePerSeat)
        {
            if (quantity is null || quantity <= 0)
                return (false, "quantity is required for seat-based plans.");
            subscription.Quantity = quantity;
        }
        else
        {
            if (quantity is not null)
                return (false, "quantity must be null for non-seat-based plans.");
        }

        subscription.SaasSubscriptionStatus = SaasSubscriptionStatus.Subscribed;
        subscription.Term.StartDate = DateTime.UtcNow;
        subscription.Term.EndDate = subscription.Term.CalculateEndDate();

        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<Operation?> ChangePlanAsync(Guid subscriptionId, string newPlanId)
    {
        var subscription = await GetAsync(subscriptionId);
        if (subscription is null)
            return null;

        if (subscription.SaasSubscriptionStatus != SaasSubscriptionStatus.Subscribed)
            return null;

        if (subscription.PlanId == newPlanId)
            return null;

        var planExists = await db.Plans.AnyAsync(p => p.PlanId == newPlanId && p.OfferId == subscription.OfferId);
        if (!planExists)
            return null;

        var operation = new Operation
        {
            SubscriptionId = subscriptionId,
            OfferId = subscription.OfferId,
            PublisherId = subscription.PublisherId,
            PlanId = newPlanId,
            Quantity = subscription.Quantity,
            Action = OperationAction.ChangePlan,
            Status = OperationStatus.InProgress
        };

        db.Operations.Add(operation);
        await db.SaveChangesAsync();
        return operation;
    }

    public async Task<Operation?> ChangeQuantityAsync(Guid subscriptionId, int newQuantity)
    {
        var subscription = await GetAsync(subscriptionId);
        if (subscription is null)
            return null;

        if (subscription.SaasSubscriptionStatus != SaasSubscriptionStatus.Subscribed)
            return null;

        if (newQuantity <= 0 || subscription.Quantity == newQuantity)
            return null;

        var operation = new Operation
        {
            SubscriptionId = subscriptionId,
            OfferId = subscription.OfferId,
            PublisherId = subscription.PublisherId,
            PlanId = subscription.PlanId,
            Quantity = newQuantity,
            Action = OperationAction.ChangeQuantity,
            Status = OperationStatus.InProgress
        };

        db.Operations.Add(operation);
        await db.SaveChangesAsync();
        return operation;
    }

    public async Task<Operation?> UnsubscribeAsync(Guid subscriptionId)
    {
        var subscription = await GetAsync(subscriptionId);
        if (subscription is null)
            return null;

        if (subscription.SaasSubscriptionStatus == SaasSubscriptionStatus.Unsubscribed)
            return null;

        var operation = new Operation
        {
            SubscriptionId = subscriptionId,
            OfferId = subscription.OfferId,
            PublisherId = subscription.PublisherId,
            PlanId = subscription.PlanId,
            Quantity = subscription.Quantity,
            Action = OperationAction.Unsubscribe,
            Status = OperationStatus.InProgress
        };

        db.Operations.Add(operation);
        await db.SaveChangesAsync();
        return operation;
    }
}
