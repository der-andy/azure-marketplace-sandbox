using System.ComponentModel.DataAnnotations;

namespace AzureMarketplaceSandbox.Domain.Models;

public class MarketplaceToken
{
    [Key]
    public int Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public Guid SubscriptionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);

    public bool IsResolved { get; set; }
}
