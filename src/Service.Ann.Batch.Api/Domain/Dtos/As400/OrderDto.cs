using System.Text.Json.Serialization;

namespace Service.Ann.Batch.Api.Domain.Dtos.As400;

public class OrderDto
{

    [JsonPropertyName("foOrder")]
    public int FoOrder { get; set; }

    [JsonPropertyName("ordSts")]
    public string OrdSts { get; set; } = "";

    [JsonPropertyName("ordStsDescription")]
    public string OrdStsDescription { get; set; } = "";

    [JsonPropertyName("lineStatus")]
    public string LineStatus { get; set; } = "";

    [JsonPropertyName("lineStatusDescription")]
    public string LineStatusDescription { get; set; } = "";

    [JsonPropertyName("mfgPlanner")]
    public string MfgPlanner { get; set; } = "";

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = "";

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = "";

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = "";

    [JsonPropertyName("orderTotal")]
    public decimal OrderTotal { get; set; }

    [JsonPropertyName("payments")]
    public decimal Payments { get; set; }

    [JsonPropertyName("balanceDue")]
    public decimal BalanceDue { get; set; }

    [JsonPropertyName("shipTrackingNum")]
    public string ShipTrackingNum { get; set; } = "";

    [JsonPropertyName("shippingMthd")]
    public string ShippingMthd { get; set; } = "";

    [JsonPropertyName("bookDt")]
    public DateTime? BookDt { get; set; }

    [JsonPropertyName("shipDt")]
    public DateTime? ShipDt { get; set; }

    [JsonPropertyName("shippingDt")]
    public DateTime? ShippingDt { get; set; }

    [JsonPropertyName("deliveryDt")]
    public DateTime? DeliveryDt { get; set; }

}
