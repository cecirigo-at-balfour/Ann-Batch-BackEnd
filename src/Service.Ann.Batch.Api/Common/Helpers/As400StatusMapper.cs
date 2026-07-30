namespace Service.Ann.Batch.Api.Common.Helpers;

public static class As400StatusMapper
{
    /// <summary>
    /// Specialized mapping for CLNWCPP (Field: NWT8SA / Alias: Line_Status).
    /// Focuses on the specific status of an item line in the fulfillment process.
    /// </summary>
    public static string ToLineStatusDescription(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "No Status";

        return status.Trim() switch
        {
            "PS" => "Preparing for Shipment", // New: Packing Slip / Pre-Ship
            "S" => "Shipped",
            "I" => "Invoiced",
            "B" => "Backordered",
            "C" => "Cancelled",
            "R" => "Returned",
            "A" => "Approved",
            "M" => "In Manufacturing",
            "F" => "Finished Goods",

            _ => $"Line ({status})"
        };
    }

    /// <summary>
    /// Specialized mapping for CLLOCPP (Field: LOMVSA / Alias: Ord_Sts).
    /// </summary>
    public static string ToOrdStsDescription(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "Unknown";

        return status.Trim() switch
        {
            "S" => "Shipped",
            "I" or "i" => "Invoiced",
            "P" => "In Progress",
            "C" or "c" => "Cancelled",
            "R" => "Returned",
            "B" => "Backordered",
            "V" or "v" => "Voided",
            _ => $"Other ({status})"
        };
    }

    /// <summary>
    /// Specific mapping for CLA6CPP and General Order Statuses.
    /// </summary>
    public static string ToOrderStatusDesciption(string? statusCode)
    {
        if (string.IsNullOrWhiteSpace(statusCode)) return "Unknown";

        return statusCode.Trim() switch
        {
            // Core Statuses
            "A" or "Active" => "Active",
            "P" => "Pending",
            "S" => "Shipped",
            "I" or "i" => "Invoiced",
            "C" or "c" => "Completed",
            "V" or "v" => "Void / Voided",
            "R" => "Returned",
            "L" => "Last Stage / Closed",
            "M" => "Manufacturing",
            "G" => "Generated",
            "B" => "Backordered",

            // Specialized / Composite Statuses
            "CA" => "Cancelled",
            "CG" => "Cancelled - General",
            "CM" => "Cancelled - Manufacturing",
            "DM" => "Damaged / Maintenance",

            // Default fallback
            _ => $"Status ({statusCode})"
        };
    }
}