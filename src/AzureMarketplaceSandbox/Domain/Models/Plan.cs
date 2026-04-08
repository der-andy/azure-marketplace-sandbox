using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzureMarketplaceSandbox.Domain.Models;

public class Plan
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    [JsonPropertyName("planId")]
    public string PlanId { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("isPrivate")]
    public bool IsPrivate { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("minQuantity")]
    public int MinQuantity { get; set; }

    [JsonPropertyName("maxQuantity")]
    public int MaxQuantity { get; set; } = 100;

    [JsonPropertyName("hasFreeTrials")]
    public bool HasFreeTrials { get; set; }

    [JsonPropertyName("isPricePerSeat")]
    public bool IsPricePerSeat { get; set; }

    [JsonPropertyName("isStopSell")]
    public bool IsStopSell { get; set; }

    [JsonPropertyName("market")]
    public string Market { get; set; } = "US";

    [JsonIgnore]
    public string BillingTermUnit { get; set; } = "P1M";

    [JsonIgnore]
    public string SubscriptionTermUnit { get; set; } = "P1M";

    [JsonIgnore]
    public List<PlanMeteringDimension> PlanMeteringDimensions { get; set; } = [];

    [JsonPropertyName("meteringDimensions")]
    public List<MeteringDimension> MeteringDimensions =>
        PlanMeteringDimensions.Select(pmd => pmd.MeteringDimension).ToList();

    [JsonIgnore]
    public string OfferId { get; set; } = string.Empty;

    [JsonIgnore]
    public Offer Offer { get; set; } = null!;
}
