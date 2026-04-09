using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AzureMarketplaceSandbox.Domain.Enums;

namespace AzureMarketplaceSandbox.Domain.Models;

public class Operation
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    [JsonPropertyName("id")]
    public Guid OperationId { get; set; } = Guid.NewGuid();

    [JsonPropertyName("activityId")]
    public Guid ActivityId { get; set; } = Guid.NewGuid();

    [JsonPropertyName("subscriptionId")]
    public Guid SubscriptionId { get; set; }

    [JsonPropertyName("offerId")]
    public string OfferId { get; set; } = string.Empty;

    [JsonPropertyName("publisherId")]
    public string PublisherId { get; set; } = string.Empty;

    [JsonPropertyName("planId")]
    public string PlanId { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    [JsonPropertyName("action")]
    public OperationAction Action { get; set; }

    [JsonPropertyName("timeStamp")]
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("status")]
    public OperationStatus Status { get; set; } = OperationStatus.InProgress;

    [JsonPropertyName("errorStatusCode")]
    public string ErrorStatusCode { get; set; } = string.Empty;

    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;

}
