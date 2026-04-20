namespace AzureMarketplaceSandbox.Services;

public interface ITenantContext
{
    int? TenantId { get; }

    Guid? EntraObjectId { get; }

    void Set(int tenantId, Guid entraObjectId);
}

public class TenantContext : ITenantContext
{
    public int? TenantId { get; private set; }

    public Guid? EntraObjectId { get; private set; }

    public void Set(int tenantId, Guid entraObjectId)
    {
        TenantId = tenantId;
        EntraObjectId = entraObjectId;
    }
}
