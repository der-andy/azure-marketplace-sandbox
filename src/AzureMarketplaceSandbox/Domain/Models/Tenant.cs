namespace AzureMarketplaceSandbox.Domain.Models;

public class Tenant
{
    public int Id { get; set; }

    public Guid EntraObjectId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? UserPrincipalName { get; set; }

    public string ApiBearerToken { get; set; } = string.Empty;

    public string PublisherId { get; set; } = string.Empty;

    public string WebhookUrl { get; set; } = string.Empty;

    public string LandingPageUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
}
