namespace Service.Ann.Batch.Api.Domain.Dtos.As400;

public class ShippingAddressDto
{
    public decimal FoOrder { get; set; }
    public decimal ShipDate { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string SchoolName { get; set; } = string.Empty;
    public string AddressName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string AddressLine3 { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public string CustomerTrackingNumber { get; set; } = string.Empty;
}