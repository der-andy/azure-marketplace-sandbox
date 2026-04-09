using System.Text.Json.Serialization;

namespace AzureMarketplaceSandbox.Domain.Models;

public class Offer
{
    [JsonIgnore]
    public int Id { get; set; }

    [JsonPropertyName("offerId")]
    public string OfferId { get; set; } = string.Empty;

    [JsonPropertyName("publisherId")]
    public string PublisherId { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "EUR";

    [JsonPropertyName("plans")]
    public List<Plan> Plans { get; set; } = [];

    [JsonIgnore]
    public List<MeteringDimension> MeteringDimensions { get; set; } = [];
}
