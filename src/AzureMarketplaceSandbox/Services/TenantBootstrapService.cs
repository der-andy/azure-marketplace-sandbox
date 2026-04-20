using System.Security.Claims;
using System.Security.Cryptography;
using AzureMarketplaceSandbox.Configuration;
using AzureMarketplaceSandbox.Data;
using AzureMarketplaceSandbox.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AzureMarketplaceSandbox.Services;

public class TenantBootstrapService(
    MarketplaceDbContext db,
    ITenantContext tenantContext,
    TenantSeedService seedService,
    IOptions<SandboxOptions> sandboxOptions,
    ILogger<TenantBootstrapService> logger)
{
    public async Task<Tenant> BootstrapAsync(Guid entraObjectId, ClaimsPrincipal user)
    {
        var displayName = user.FindFirst("name")?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? "Unknown User";
        var upn = user.FindFirst(ClaimTypes.Upn)?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value;

        var defaults = sandboxOptions.Value;
        var tenant = new Tenant
        {
            EntraObjectId = entraObjectId,
            DisplayName = displayName,
            UserPrincipalName = upn,
            ApiBearerToken = GenerateBearerToken(),
            PublisherId = DerivePublisherId(upn),
            WebhookUrl = defaults.WebhookUrl,
            LandingPageUrl = defaults.LandingPageUrl,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        };
        db.Tenants.Add(tenant);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // A concurrent request already created the tenant — load and return it.
            db.ChangeTracker.Clear();
            return await db.Tenants.IgnoreQueryFilters()
                .FirstAsync(t => t.EntraObjectId == entraObjectId);
        }

        tenantContext.Set(tenant.Id, tenant.EntraObjectId);
        await seedService.SeedAsync(tenant);

        logger.LogInformation("Bootstrapped tenant {TenantId} for {Upn} (oid {Oid}).",
            tenant.Id, upn, entraObjectId);
        return tenant;
    }

    internal static string DerivePublisherId(string? upn)
    {
        if (string.IsNullOrWhiteSpace(upn))
            return "my-publisher";
        var user = upn.Split('@', 2)[0];
        return user.Replace('.', '-').ToLowerInvariant();
    }

    internal static string GenerateBearerToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
