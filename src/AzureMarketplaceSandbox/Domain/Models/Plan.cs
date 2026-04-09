using System.Text.Json.Serialization;

namespace AzureMarketplaceSandbox.Domain.Models;

public class Plan
{
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
    public int MaxQuantity { get; set; }

    [JsonPropertyName("hasFreeTrials")]
    public bool HasFreeTrials { get; set; }

    [JsonPropertyName("isPricePerSeat")]
    public bool IsPricePerSeat { get; set; }

    [JsonPropertyName("isStopSell")]
    public bool IsStopSell { get; set; }

    [JsonPropertyName("market")]
    public string Market { get; set; } = "DE";

    [JsonIgnore]
    public string BillingTermUnit { get; set; } = "P1M";

    [JsonIgnore]
    public string SubscriptionTermUnit { get; set; } = "P1M";

    [JsonIgnore]
    public decimal Price { get; set; }

    [JsonIgnore]
    public string TermDescription { get; set; } = string.Empty;

    [JsonIgnore]
    public List<PlanMeteringDimension> PlanMeteringDimensions { get; set; } = [];

    [JsonPropertyName("planComponents")]
    public PlanComponents PlanComponents => new()
    {
        RecurrentBillingTerms =
        [
            new RecurrentBillingTerm
            {
                Currency = Offer?.Currency ?? "EUR",
                Price = Price,
                TermUnit = SubscriptionTermUnit,
                TermDescription = TermDescription,
                MeteredQuantityIncluded = PlanMeteringDimensions
                    .Where(pmd => pmd.MeteringDimension is not null)
                    .Select(pmd => new MeteredQuantityIncluded
                    {
                        DimensionId = pmd.MeteringDimension.DimensionId,
                        Units = pmd.IncludedQuantity
                    }).ToList()
            }
        ],
        MeteringDimensions = PlanMeteringDimensions
            .Where(pmd => pmd.MeteringDimension is not null)
            .Select(pmd => pmd.MeteringDimension).ToList()
    };

    [JsonIgnore]
    public Offer Offer { get; set; } = null!;
}
