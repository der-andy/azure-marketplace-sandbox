using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AzureMarketplaceSandbox.Domain.Enums;

namespace AzureMarketplaceSandbox.Domain.Models;

public class UsageEvent
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    [JsonPropertyName("usageEventId")]
    public Guid UsageEventId { get; set; } = Guid.NewGuid();

    [JsonPropertyName("status")]
    public UsageEventStatus Status { get; set; } = UsageEventStatus.Accepted;

    [JsonPropertyName("messageTime")]
    public DateTime MessageTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("resourceId")]
    public Guid ResourceId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("dimension")]
    public string Dimension { get; set; } = string.Empty;

    [JsonPropertyName("effectiveStartTime")]
    public DateTime EffectiveStartTime { get; set; }

    [JsonPropertyName("planId")]
    public string PlanId { get; set; } = string.Empty;
}
