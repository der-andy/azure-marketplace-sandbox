using System.Text.Json.Serialization;

namespace AzureMarketplaceSandbox.Domain.Models;

public class PlanMeteringDimension
{
    [JsonIgnore]
    public int PlanId { get; set; }

    [JsonIgnore]
    public Plan Plan { get; set; } = null!;

    [JsonIgnore]
    public int MeteringDimensionId { get; set; }

    [JsonIgnore]
    public MeteringDimension MeteringDimension { get; set; } = null!;

    [JsonIgnore]
    public decimal IncludedQuantity { get; set; }
}
