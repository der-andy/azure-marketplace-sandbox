using System.Text.Json.Serialization;

namespace AzureMarketplaceSandbox.Domain.Models;

public class MeteringDimension
{
    [JsonIgnore]
    public int Id { get; set; }

    [JsonPropertyName("id")]
    public string DimensionId { get; set; } = string.Empty;

    [JsonPropertyName("pricePerUnit")]
    public decimal PricePerUnit { get; set; }

    [JsonPropertyName("unitOfMeasure")]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency => Offer?.Currency ?? "EUR";

    [JsonIgnore]
    public Offer Offer { get; set; } = null!;

    [JsonIgnore]
    public List<PlanMeteringDimension> PlanMeteringDimensions { get; set; } = [];
}
