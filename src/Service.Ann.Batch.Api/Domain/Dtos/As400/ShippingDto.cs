using System.Text.Json.Serialization;

namespace Service.Ann.Batch.Api.Domain.Dtos.As400;
public class ShippingDto
{
  
    [JsonPropertyName("itemCode")]
    public string ItemCode { get; set; } = "";

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = "";

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = "";

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = "";

    [JsonPropertyName("schoolName")]
    public string SchoolName { get; set; } = "";

    [JsonPropertyName("addressName")]
    public string AddressName { get; set; } = "";

    [JsonPropertyName("addressLine1")]
    public string AddressLine1 { get; set; } = "";

    [JsonPropertyName("addressLine2")]
    public string AddressLine2 { get; set; } = "";

    [JsonPropertyName("addressLine3")]
    public string AddressLine3 { get; set; } = "";

    [JsonPropertyName("zipCode")]
    public string ZipCode { get; set; } = "";

    [JsonPropertyName("city")]
    public string City { get; set; } = "";

    [JsonPropertyName("state")]
    public string State { get; set; } = "";

    [JsonPropertyName("trackingNumber")]
    public string TrackingNumber { get; set; } = "";

    [JsonPropertyName("customerTrackingNumber")]
    public string CustomerTrackingNumber { get; set; } = "";

    [JsonPropertyName("shippingDt")]
    public DateTime? ShippingDt { get; set; }
}
