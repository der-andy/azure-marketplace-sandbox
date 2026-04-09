using AzureMarketplaceSandbox.Services;

namespace AzureMarketplaceSandbox.Api;

public static class FulfillmentOperationsEndpoints
{
    public static void MapFulfillmentOperationsApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/saas/subscriptions/{subscriptionId:guid}/operations")
            .RequireAuthorization(policy => policy
                .AddAuthenticationSchemes(Auth.SandboxBearerHandler.SchemeName)
                .RequireAuthenticatedUser());

        // GET / (list pending operations)
        group.MapGet("/", async (Guid subscriptionId, OperationService operationService) =>
        {
            var operations = await operationService.ListPendingAsync(subscriptionId);
            return Results.Ok(new { operations });
        });

        // GET /{operationId}
        group.MapGet("/{operationId:guid}", async (
            Guid subscriptionId,
            Guid operationId,
            OperationService operationService) =>
        {
            var operation = await operationService.GetAsync(subscriptionId, operationId);
            if (operation is null)
                return Results.NotFound();

            return Results.Ok(operation);
        });

        // PATCH /{operationId} (update status: Success or Failure)
        group.MapPatch("/{operationId:guid}", async (
            Guid subscriptionId,
            Guid operationId,
            HttpContext context,
            OperationService operationService) =>
        {
            var body = await context.Request.ReadFromJsonAsync<PatchOperationRequest>();
            if (body is null || string.IsNullOrWhiteSpace(body.Status))
            {
                return Results.BadRequest(new { message = "Request body with 'status' field is required.", code = "BadArgument" });
            }

            var (found, updated, error) = await operationService.UpdateStatusAsync(subscriptionId, operationId, body.Status);

            if (!found)
                return Results.NotFound();

            if (!updated)
                return Results.Conflict(new { message = error ?? "Operation could not be updated.", code = "Conflict" });

            return Results.Ok();
        });
    }

    private record PatchOperationRequest
    {
        public string? Status { get; init; }
    }
}
