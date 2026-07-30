namespace Service.Ann.Batch.Api.Application.Dtos;

public class AnnouncementDto
{
    /// <summary>
    /// Original order number from the Front Office (FO) system.
    /// </summary>
    /// <example>123456</example>
    public long FoOrder { get; set; }

    /// <summary>
    /// Current order status code (Header level from CLLOCPP).
    /// </summary>
    /// <example>S</example>
    public string OrdSts { get; set; } = string.Empty;

    /// <summary>
    /// Friendly description for the Order Status.
    /// </summary>
    /// <example>Shipped</example>
    public string OrdStsDescription { get; set; } = string.Empty;

    /// <summary>
    /// Specific status code for the item line (From CLNWCPP).
    /// </summary>
    /// <example>PS</example>
    public string LineStatus { get; set; } = string.Empty;

    /// <summary>
    /// Friendly description for the Line Status.
    /// </summary>
    /// <example>Preparing for Shipment</example>
    public string LineStatusDescription { get; set; } = string.Empty;

    /// <summary>
    /// Manufacturing planner identifier.
    /// </summary>
    /// <example>ANN</example>
    public string MfgPlanner { get; set; } = string.Empty;

    /// <summary>
    /// Customer's first name.
    /// </summary>
    /// <example>Jhon</example>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Customer's last name.
    /// </summary>
    /// <example>Doe</example>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Computed property returning the customer's full name.
    /// </summary>
    /// <example>Jhon Doe</example>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Total amount of the order.
    /// </summary>
    /// <example>500.00</example>
    public decimal OrderTotal { get; set; }

    /// <summary>
    /// Total amount paid to date.
    /// </summary>
    /// <example>350.00</example>
    public decimal Payments { get; set; }

    /// <summary>
    /// Remaining balance to be paid.
    /// </summary>
    /// <example>150.00</example>
    public decimal BalanceDue { get; set; }

    /// <summary>
    /// Carrier tracking number for the shipment.
    /// </summary>
    /// <example>1Z999AA10123456789</example>
    public string ShipTrackingNum { get; set; } = string.Empty;

    /// <summary>
    /// Shipping method or carrier name.
    /// </summary>
    /// <example>UPS GROUND</example>
    public string ShippingMthd { get; set; } = string.Empty;

    // --- Formatted Date Properties (ISO 8601) ---

    /// <summary>
    /// Order registration date (Book Date).
    /// </summary>
    /// <example>2026-03-26</example>
    public DateOnly? BookDt { get; set; }

    /// <summary>
    /// Estimated or actual shipment date.
    /// </summary>
    /// <example>2026-03-28</example>
    public DateOnly? ShipDt { get; set; }

    /// <summary>
    /// Internal shipping processing date.
    /// </summary>
    /// <example>2026-03-29</example>
    public DateOnly? ShippingDt { get; set; }

    /// <summary>
    /// Confirmed delivery date at destination.
    /// </summary>
    /// <example>2026-04-01</example>
    public DateOnly? DeliveryDt { get; set; }
}