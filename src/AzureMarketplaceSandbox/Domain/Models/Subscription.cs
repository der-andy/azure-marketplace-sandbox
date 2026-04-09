using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using AzureMarketplaceSandbox.Domain.Enums;

namespace AzureMarketplaceSandbox.Domain.Models;

public class Subscription
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    [JsonPropertyName("id")]
    public Guid SubscriptionId { get; set; } = Guid.NewGuid();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("publisherId")]
    public string PublisherId { get; set; } = string.Empty;

    [JsonPropertyName("offerId")]
    public string OfferId { get; set; } = string.Empty;

    [JsonPropertyName("planId")]
    public string PlanId { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    [JsonPropertyName("beneficiary")]
    public AadInfo Beneficiary { get; set; } = new();

    [JsonPropertyName("purchaser")]
    public AadInfo Purchaser { get; set; } = new();

    [JsonPropertyName("allowedCustomerOperations")]
    [NotMapped]
    public string[] AllowedCustomerOperations { get; set; } = ["Read", "Update", "Delete"];

    [JsonPropertyName("sessionMode")]
    public string SessionMode { get; set; } = "None";

    [JsonPropertyName("isFreeTrial")]
    public bool IsFreeTrial { get; set; }

    [JsonPropertyName("isTest")]
    public bool IsTest { get; set; }

    [JsonPropertyName("sandboxType")]
    public string SandboxType { get; set; } = "None";

    [JsonPropertyName("saasSubscriptionStatus")]
    public SaasSubscriptionStatus SaasSubscriptionStatus { get; set; } = SaasSubscriptionStatus.PendingFulfillmentStart;

    [JsonPropertyName("term")]
    public SubscriptionTerm Term { get; set; } = new();

    [JsonPropertyName("autoRenew")]
    public bool AutoRenew { get; set; } = true;

    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

}
