namespace Service.Ann.Batch.Api.Domain.Entities;

public class BatchEntity : GenericAuditEntity
{
    public DateTime BatchDate { get; set; }

    public string Fo { get; set; } = "";
    public string Item { get; set; } = "";
    public string School { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string? StudentLastName { get; set; }

    public string Sr { get; set; } = "";
    public bool? Srapproved { get; set; }
    public DateTime? Srapprdate { get; set; }
    public string Magento { get; set; } = "";
    public string Po { get; set; } = "";
    public string Uuid { get; set; } = "";

    public string? CustomerTrackingNumber { get; set; }

    public string? ShipTracking { get; set; }
    public string? ShipMethod { get; set; }
    public DateTime? ShipDate { get; set; }

    public string? AddressName { get; set; }
    public string? ShipAddress { get; set; }
    public string? ShipAddress2 { get; set; }
    public string? ShipAddress3 { get; set; }

    public string? ShipCity { get; set; }
    public string? ShipState { get; set; }
    public string? ShipCode { get; set; }

    public DateTime? DeliveryDate { get; set; }
    public DateTime? BookDate { get; set; }

    public string? Files { get; set; }
    public DateTime? FilesDate { get; set; }

    public string? Priority { get; set; }

    public string? LineStatus { get; set; }
    public string? Status { get; set; }

    public decimal? OrderTotal { get; set; }
    public decimal? Payments { get; set; }
    public decimal? BalanceDue { get; set; }
    public int? Quantity { get; set; }

    //  public virtual IEnumerable<Annfile>AnnFiles { get; set; }
}
