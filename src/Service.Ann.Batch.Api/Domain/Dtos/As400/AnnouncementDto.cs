namespace Service.Ann.Batch.Api.Domain.Dtos.As400;

public class AnnouncementDto
{
    public decimal FoOrder { get; set; }
    public string OrdSts { get; set; } = string.Empty;
    public string MfgPlanner { get; set; } = string.Empty;
    public decimal BookDt { get; set; }
    public decimal ShipDt { get; set; }
    public decimal CancelDt { get; set; }
    public decimal ReturnDt { get; set; }
    public string Class { get; set; } = string.Empty;
    public string XRef { get; set; } = string.Empty;
    public string Po { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public decimal OrderTotal { get; set; }
    public decimal Payments { get; set; }
    public decimal BalanceDue { get; set; }
    public string PaymentTerm { get; set; } = string.Empty;
    public decimal Surrogate { get; set; }
    public string SchoolState { get; set; } = string.Empty;
    public string SchoolNum { get; set; } = string.Empty;
    public string SellingStore { get; set; } = string.Empty;
    public decimal SellingLn { get; set; }
    public string BillingStore { get; set; } = string.Empty;
    public decimal BillingLine { get; set; }
    public decimal LineSequence { get; set; }
    public string LineStatus { get; set; } = string.Empty;
    public decimal StatusDt { get; set; }
    public string ItemCd { get; set; } = string.Empty;
    public string MetalId { get; set; } = string.Empty;
    public string PmClass { get; set; } = string.Empty;
    public string ProdLine { get; set; } = string.Empty;
    public decimal GroupShip { get; set; }
    public decimal GroupShipSeq { get; set; }
    public string ShipTrackingNum { get; set; } = string.Empty;
    public string ShippingMthd { get; set; } = string.Empty;
    public decimal ShippingDt { get; set; }
    public decimal DeliveryDt { get; set; }
}