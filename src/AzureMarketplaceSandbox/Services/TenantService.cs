using AzureMarketplaceSandbox.Data;
using AzureMarketplaceSandbox.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AzureMarketplaceSandbox.Services;

public class TenantService(MarketplaceDbContext db, ITenantContext tenantContext)
{
    public async Task<Tenant?> GetCurrentAsync()
    {
        if (tenantContext.TenantId is not int id) return null;
        return await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task UpdatePublisherIdAsync(string publisherId)
    {
        var tenant = await GetCurrentAsync()
            ?? throw new InvalidOperationException("No current tenant.");
        tenant.PublisherId = publisherId.Trim();
        await db.SaveChangesAsync();
    }

    public async Task<string> RegenerateApiBearerTokenAsync()
    {
        var tenant = await GetCurrentAsync()
            ?? throw new InvalidOperationException("No current tenant.");
        tenant.ApiBearerToken = TenantBootstrapService.GenerateBearerToken();
        await db.SaveChangesAsync();
        return tenant.ApiBearerToken;
    }
}
