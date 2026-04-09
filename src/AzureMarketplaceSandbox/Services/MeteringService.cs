using AzureMarketplaceSandbox.Data;
using AzureMarketplaceSandbox.Domain.Enums;
using AzureMarketplaceSandbox.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AzureMarketplaceSandbox.Services;

public class MeteringService(MarketplaceDbContext db)
{
    public async Task<UsageEvent> PostUsageEventAsync(UsageEventRequest request)
    {
        var usageEvent = new UsageEvent
        {
            ResourceId = request.ResourceId,
            Quantity = request.Quantity,
            Dimension = request.Dimension,
            EffectiveStartTime = request.EffectiveStartTime,
            PlanId = request.PlanId,
            MessageTime = DateTime.UtcNow
        };

        var validationError = await ValidateAsync(request);
        if (validationError is not null)
        {
            usageEvent.Status = validationError.Value;
            return usageEvent;
        }

        // Duplicate check: same resource + dimension + same calendar hour
        var duplicate = await FindDuplicateAsync(request);
        if (duplicate is not null)
        {
            usageEvent.Status = UsageEventStatus.Duplicate;
            return usageEvent;
        }

        usageEvent.Status = UsageEventStatus.Accepted;
        db.UsageEvents.Add(usageEvent);
        await db.SaveChangesAsync();
        return usageEvent;
    }

    public async Task<List<UsageEvent>> PostBatchUsageEventsAsync(List<UsageEventRequest> requests)
    {
        var results = new List<UsageEvent>();
        foreach (var request in requests)
        {
            results.Add(await PostUsageEventAsync(request));
        }
        return results;
    }

    public async Task<List<UsageEvent>> GetUsageEventsAsync(
        DateTime usageStartDate,
        DateTime? usageEndDate = null,
        string? offerId = null,
        string? planId = null,
        string? dimension = null)
    {
        var query = db.UsageEvents
            .Where(e => e.EffectiveStartTime >= usageStartDate);

        if (usageEndDate.HasValue)
            query = query.Where(e => e.EffectiveStartTime <= usageEndDate.Value);

        if (offerId is not null)
        {
            var subscriptionIds = await db.Subscriptions
                .Where(s => s.OfferId == offerId)
                .Select(s => s.SubscriptionId)
                .ToListAsync();
            query = query.Where(e => subscriptionIds.Contains(e.ResourceId));
        }

        if (planId is not null)
            query = query.Where(e => e.PlanId == planId);

        if (dimension is not null)
            query = query.Where(e => e.Dimension == dimension);

        return await query.OrderByDescending(e => e.EffectiveStartTime).ToListAsync();
    }

    private async Task<UsageEventStatus?> ValidateAsync(UsageEventRequest request)
    {
        if (request.Quantity <= 0)
            return UsageEventStatus.InvalidQuantity;

        if (string.IsNullOrWhiteSpace(request.Dimension))
            return UsageEventStatus.BadArgument;

        if (request.EffectiveStartTime > DateTime.UtcNow)
            return UsageEventStatus.BadArgument;

        if (request.EffectiveStartTime < DateTime.UtcNow.AddHours(-24))
            return UsageEventStatus.Expired;

        var subscription = await db.Subscriptions.FirstOrDefaultAsync(s => s.SubscriptionId == request.ResourceId);
        if (subscription is null)
            return UsageEventStatus.ResourceNotFound;

        if (subscription.SaasSubscriptionStatus != SaasSubscriptionStatus.Subscribed)
            return UsageEventStatus.ResourceNotActive;

        // Validate dimension exists and is assigned to the plan
        var dimensionExists = await db.PlanMeteringDimensions
            .AnyAsync(pmd => pmd.MeteringDimension.DimensionId == request.Dimension &&
                             pmd.Plan.PlanId == request.PlanId &&
                             pmd.Plan.Offer.OfferId == subscription.OfferId);
        if (!dimensionExists)
            return UsageEventStatus.InvalidDimension;

        return null;
    }

    private async Task<UsageEvent?> FindDuplicateAsync(UsageEventRequest request)
    {
        var est = request.EffectiveStartTime;

        var allEvents = await db.UsageEvents.ToListAsync();

        return allEvents.FirstOrDefault(e =>
            e.ResourceId == request.ResourceId &&
            e.Dimension == request.Dimension &&
            e.EffectiveStartTime.Year == est.Year &&
            e.EffectiveStartTime.Month == est.Month &&
            e.EffectiveStartTime.Day == est.Day &&
            e.EffectiveStartTime.Hour == est.Hour);
    }
}

public record UsageEventRequest
{
    public Guid ResourceId { get; init; }
    public decimal Quantity { get; init; }
    public string Dimension { get; init; } = string.Empty;
    public DateTime EffectiveStartTime { get; init; }
    public string PlanId { get; init; } = string.Empty;
}
