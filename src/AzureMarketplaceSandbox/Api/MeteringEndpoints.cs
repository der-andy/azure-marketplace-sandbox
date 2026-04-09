using AzureMarketplaceSandbox.Domain.Enums;
using AzureMarketplaceSandbox.Services;

namespace AzureMarketplaceSandbox.Api;

public static class MeteringEndpoints
{
    public static void MapMeteringApi(this WebApplication app)
    {
        // POST /api/usageEvent
        app.MapPost("/api/usageEvent", async (
            UsageEventRequest request,
            MeteringService meteringService) =>
        {
            var result = await meteringService.PostUsageEventAsync(request);

            return result.Status switch
            {
                UsageEventStatus.Accepted => Results.Ok(result),
                UsageEventStatus.Duplicate => Results.Conflict(new
                {
                    additionalInfo = new { acceptedMessage = result },
                    message = "This usage event already exist.",
                    code = "Conflict"
                }),
                UsageEventStatus.BadArgument or UsageEventStatus.InvalidQuantity => Results.BadRequest(new
                {
                    message = "One or more errors have occurred.",
                    target = "usageEventRequest",
                    details = new[] { new { message = $"Validation failed: {result.Status}", target = result.Status.ToString(), code = "BadArgument" } },
                    code = "BadArgument"
                }),
                _ => Results.BadRequest(new
                {
                    message = $"Usage event rejected: {result.Status}",
                    code = result.Status.ToString()
                })
            };
        }).RequireAuthorization(policy => policy
            .AddAuthenticationSchemes(Auth.SandboxBearerHandler.SchemeName)
            .RequireAuthenticatedUser());

        // POST /api/batchUsageEvent
        app.MapPost("/api/batchUsageEvent", async (
            BatchUsageEventRequest request,
            MeteringService meteringService) =>
        {
            if (request.Request is null || request.Request.Count == 0)
            {
                return Results.BadRequest(new { message = "Request body must contain usage events.", code = "BadArgument" });
            }

            if (request.Request.Count > 25)
            {
                return Results.BadRequest(new { message = "The batch contained more than 25 usage events.", code = "BadArgument" });
            }

            var results = await meteringService.PostBatchUsageEventsAsync(request.Request);

            return Results.Ok(new
            {
                count = results.Count,
                result = results.Select(r => r.Status == UsageEventStatus.Accepted
                    ? (object)r
                    : new
                    {
                        status = r.Status,
                        messageTime = r.MessageTime,
                        error = new
                        {
                            additionalInfo = r.Status == UsageEventStatus.Duplicate
                                ? new { acceptedMessage = r }
                                : null,
                            message = r.Status == UsageEventStatus.Duplicate
                                ? "This usage event already exist."
                                : $"Usage event rejected: {r.Status}",
                            code = r.Status.ToString()
                        },
                        resourceId = r.ResourceId,
                        quantity = r.Quantity,
                        dimension = r.Dimension,
                        effectiveStartTime = r.EffectiveStartTime,
                        planId = r.PlanId
                    }).ToList()
            });
        }).RequireAuthorization(policy => policy
            .AddAuthenticationSchemes(Auth.SandboxBearerHandler.SchemeName)
            .RequireAuthenticatedUser());

        // GET /api/usageEvents
        app.MapGet("/api/usageEvents", async (
            DateTime usageStartDate,
            DateTime? usageEndDate,
            string? offerId,
            string? planId,
            string? dimension,
            MeteringService meteringService) =>
        {
            var events = await meteringService.GetUsageEventsAsync(
                usageStartDate, usageEndDate, offerId, planId, dimension);

            return Results.Ok(events);
        }).RequireAuthorization(policy => policy
            .AddAuthenticationSchemes(Auth.SandboxBearerHandler.SchemeName)
            .RequireAuthenticatedUser());
    }

    private record BatchUsageEventRequest
    {
        public List<UsageEventRequest> Request { get; init; } = [];
    }
}
