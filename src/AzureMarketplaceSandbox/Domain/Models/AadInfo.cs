using System.Text.Json.Serialization;

namespace AzureMarketplaceSandbox.Domain.Models;

public class AadInfo
{
    [JsonIgnore]
    public int Id { get; set; }

    [JsonPropertyName("emailId")]
    public string EmailId { get; set; } = string.Empty;

    [JsonPropertyName("objectId")]
    public string ObjectId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("puid")]
    public string Puid { get; set; } = string.Empty;
}
