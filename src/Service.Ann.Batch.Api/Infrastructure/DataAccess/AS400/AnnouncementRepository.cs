

using Service.Ann.Batch.Api.Application.Abstractions.AS400;
using Service.Ann.Batch.Api.Domain.Dtos.As400;
using Service.Ann.Batch.Api.Domain.Entities;
using System.Data.OleDb;

namespace Service.Ann.Batch.Api.Infrastructure.DataAccess.AS400;

public sealed class AnnouncementRepository : IAnnouncementRepository
{
    private readonly string _cs;

    public AnnouncementRepository(IConfiguration configuration)
    {
        _cs = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
    }

    /// <summary>
    /// Fetches announcement records from AS400 based on status and date range.
    /// </summary>
    /// 
   
    public async Task<IReadOnlyList<Announcement>> GetAnnouncementsAsync(
        decimal startAs400,
        decimal endAs400,
        string status,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var list = new List<Announcement>();
            using var cn = new OleDbConnection(_cs);
            cn.Open();

            using var cmd = cn.CreateCommand();

            cmd.CommandText = @"
                SELECT 
                    /* --- Order Header Information (cllocpp) --- */
                    O.LORLNX AS FO_Order,           -- FULFILL_ORDER_NUMBER (DECIMAL 7,0)
                    O.LOMVSA AS Ord_Sts,            -- FO_STATUS (CHAR 2)
                    O.LOP8DT AS Book_Dt,            -- FO_BOOKED_DATE (DECIMAL 7,0)
                    O.LOP9DT AS Ship_Dt,            -- FO_SHIPPED_DATE (DECIMAL 7,0)
                    O.LOQADT AS Cancel_Dt,          -- FO_CANCELLED_DATE (DECIMAL 7,0)
                    O.LOQBDT AS Return_Dt,          -- FO_RETURNED_DATE (DECIMAL 7,0)
                    O.LOMWSA AS Class,              -- FO_CLASS (CHAR 1)
                    O.LOPNCX AS X_REF,              -- FO_X_REF (CHAR 20)
                    O.LOPOCX AS PO,                 -- FO_P_O_NUMBER (CHAR 20)
                    O.LOQ8TT AS Last_Name,          -- FO_STUDENT_LAST_NAME (CHAR 20)
                    O.LOWWTT AS First_Name,         -- FO_STUDENT_FIRST_MIDDLE (CHAR 25)
                    O.LOTEVL AS Order_Total,        -- FO_ORDER_TOTAL (DECIMAL 9,2)
                    O.LOTLVL AS Payments,           -- FO_TOTAL_PAYMENTS (DECIMAL 9,2)
                    O.LOTMVL AS Balance_Due,        -- FO_BALANCE_DUE (DECIMAL 9,2)
                    O.LOBXCD AS Payment_Term,       -- PMT_TRM_CODE (CHAR 4)
                    O.LORTNX AS Surrogate,          -- ENV_SURROGATE_NUMBER (DECIMAL 7,0)
                    O.LOH2CD AS School_State,       -- SCHOOL_STATE (CHAR 2)
                    O.LOH1CD AS School_Num,         -- SCHOOL_NUMBER (DECIMAL 6,0)
                    O.LOPPCX AS Selling_Store,      -- FO_SELLING_STORE_NUMBER (CHAR 7)
                    O.LORSNX AS Selling_LN,         -- FO_SELLING_PRODUCT_LN (DECIMAL 2,0)
                    O.LOPQCX AS Billing_Store,      -- FO_BILLING_STORE_NUMBER (CHAR 7)
                    O.LORRNX AS Billing_Line,       -- FO_BILLING_PRODUCT_LN (DECIMAL 2,0)

                    /* --- Line Detail Information (clnwcpp) --- */
                    L.NWROCX AS Mfg_Planner,        -- MFG_PLANNER (CHAR 3)
                    L.NWW2NX AS Line_Sequnce,       -- FO_LINE_SEQ_NUMBER (DECIMAL 5,0)
                    L.NWT8SA AS Line_Status,        -- FO_LINE_STATUS (CHAR 2)
                    L.NWP7DT AS Status_Dt,          -- FO_LINE_STATUS_DATE (DECIMAL 7,0)
                    L.NWLJCD AS Item_Cd,            -- ITEM_CODE (CHAR 15)
                    L.NWH9CD AS Metal_Id,           -- METAL_CODE (CHAR 4)
                    L.NWLPST AS PM_Class,           -- PM_CLASS_TYPE (CHAR 1)
                    L.NWB4CD AS Prod_Line,          -- PRODUCT_LINE (DECIMAL 2,0)
                    L.NWF2NB AS Group_Ship,         -- GROUP_SHIP_NUMBER (DECIMAL 7,0)
                    L.NWQGNB AS Group_Ship_Seq,     -- GROUP_SHIP_SEQUENCE (DECIMAL 3,0)

                    /* --- Shipping Tracking (clracpp) --- */
                    S.RADNTU AS Ship_Tracking_Num,  -- SHP_TRK_NUMBER (CHAR 30)
                    S.RADOTU AS Shipping_MTHD,      -- SHP_TRK_ACTUAL_SHIP_MTHD (CHAR 25)
                    S.RAQPDT AS Shipping_Dt,        -- SHP_TRK_ACTUAL_SHIP_DATE (DECIMAL 7,0)
                    S.RASYDT AS Delivery_Dt         -- SHP_TRK_DELIVERY_DATE (DECIMAL 7,0)

                FROM cllocpp O
                INNER JOIN clnwcpp L 
                    ON O.LORLNX = L.NWRLNX         -- FULFILL_ORDER_NUMBER Join
                LEFT OUTER JOIN clracpp S 
                    ON S.RAARN1 = O.LORLNX         -- SHP_TRK_FO_GRP_ORD_NBR
                    AND S.RAQPDT = L.NWP7DT        -- Match by Actual Ship Date vs Line Status Date

                WHERE O.LOMVSA = ?               -- Param: FO_STATUS
                  AND L.NWROCX = 'ANN'           -- Filter for Announcements
                  AND O.LOP9DT BETWEEN ? AND ?   -- Param: FO_SHIPPED_DATE Range

                ORDER BY O.LORLNX, L.NWW2NX   -- FO_Order , Line_Sequnce
                WITH UR";

            // Parameter order is critical in OleDb
            cmd.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.Char, Value = status });
            cmd.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.Numeric, Value = startAs400 });
            cmd.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.Numeric, Value = endAs400 });

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(MapToEntity(r));
            }

            return (IReadOnlyList<Announcement>)list;
        }, ct);
    }

    /// <summary>
    /// Fetch announcement record from AS400 by foOrder.
    /// </summary>
    public async Task<Announcement?> GetByOrderAsync(long foOrder, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var cn = new OleDbConnection(_cs);
            cn.Open();
            using var cmd = cn.CreateCommand();

            cmd.CommandText = @"
                    SELECT 
                        /* --- Order Header Information (cllocpp) --- */
                        O.LORLNX AS FO_Order,           -- FULFILL_ORDER_NUMBER (DECIMAL 7,0)
                        O.LOMVSA AS Ord_Sts,            -- FO_STATUS (CHAR 2)
                        O.LOP8DT AS Book_Dt,            -- FO_BOOKED_DATE (DECIMAL 7,0)
                        O.LOP9DT AS Ship_Dt,            -- FO_SHIPPED_DATE (DECIMAL 7,0)
                        O.LOQADT AS Cancel_Dt,          -- FO_CANCELLED_DATE (DECIMAL 7,0)
                        O.LOQBDT AS Return_Dt,          -- FO_RETURNED_DATE (DECIMAL 7,0)
                        O.LOMWSA AS Class,              -- FO_CLASS (CHAR 1)
                        O.LOPNCX AS X_REF,              -- FO_X_REF (CHAR 20)
                        O.LOPOCX AS PO,                 -- FO_P_O_NUMBER (CHAR 20)
                        O.LOQ8TT AS Last_Name,          -- FO_STUDENT_LAST_NAME (CHAR 20)
                        O.LOWWTT AS First_Name,         -- FO_STUDENT_FIRST_MIDDLE (CHAR 25)
                        O.LOTEVL AS Order_Total,        -- FO_ORDER_TOTAL (DECIMAL 9,2)
                        O.LOTLVL AS Payments,           -- FO_TOTAL_PAYMENTS (DECIMAL 9,2)
                        O.LOTMVL AS Balance_Due,        -- FO_BALANCE_DUE (DECIMAL 9,2)
                        O.LOBXCD AS Payment_Term,       -- PMT_TRM_CODE (CHAR 4)
                        O.LORTNX AS Surrogate,          -- ENV_SURROGATE_NUMBER (DECIMAL 7,0)
                        O.LOH2CD AS School_State,       -- SCHOOL_STATE (CHAR 2)
                        O.LOH1CD AS School_Num,         -- SCHOOL_NUMBER (DECIMAL 6,0)
                        O.LOPPCX AS Selling_Store,      -- FO_SELLING_STORE_NUMBER (CHAR 7)
                        O.LORSNX AS Selling_LN,         -- FO_SELLING_PRODUCT_LN (DECIMAL 2,0)
                        O.LOPQCX AS Billing_Store,      -- FO_BILLING_STORE_NUMBER (CHAR 7)
                        O.LORRNX AS Billing_Line,       -- FO_BILLING_PRODUCT_LN (DECIMAL 2,0)

                        /* --- Line Detail Information (clnwcpp) --- */
                        L.NWROCX AS Mfg_Planner,        -- MFG_PLANNER (CHAR 3)
                        L.NWW2NX AS Line_Sequnce,       -- FO_LINE_SEQ_NUMBER (DECIMAL 5,0)
                        L.NWT8SA AS Line_Status,        -- FO_LINE_STATUS (CHAR 2)
                        L.NWP7DT AS Status_Dt,          -- FO_LINE_STATUS_DATE (DECIMAL 7,0)
                        L.NWLJCD AS Item_Cd,            -- ITEM_CODE (CHAR 15)
                        L.NWH9CD AS Metal_Id,           -- METAL_CODE (CHAR 4)
                        L.NWLPST AS PM_Class,           -- PM_CLASS_TYPE (CHAR 1)
                        L.NWB4CD AS Prod_Line,          -- PRODUCT_LINE (DECIMAL 2,0)
                        L.NWF2NB AS Group_Ship,         -- GROUP_SHIP_NUMBER (DECIMAL 7,0)
                        L.NWQGNB AS Group_Ship_Seq,     -- GROUP_SHIP_SEQUENCE (DECIMAL 3,0)

                        /* --- Shipping Tracking (clracpp) --- */
                        S.RADNTU AS Ship_Tracking_Num,  -- SHP_TRK_NUMBER (CHAR 30)
                        S.RADOTU AS Shipping_MTHD,      -- SHP_TRK_ACTUAL_SHIP_MTHD (CHAR 25)
                        S.RAQPDT AS Shipping_Dt,        -- SHP_TRK_ACTUAL_SHIP_DATE (DECIMAL 7,0)
                        S.RASYDT AS Delivery_Dt         -- SHP_TRK_DELIVERY_DATE (DECIMAL 7,0)

                    FROM cllocpp O
                    INNER JOIN clnwcpp L 
                        ON O.LORLNX = L.NWRLNX         -- FULFILL_ORDER_NUMBER Join
                    LEFT OUTER JOIN clracpp S 
                        ON S.RAARN1 = O.LORLNX         -- SHP_TRK_FO_GRP_ORD_NBR
                        AND S.RAQPDT = L.NWP7DT        -- Match by Ship Date vs Line Status Date

                    WHERE O.LORLNX = ?                 -- Parameter: Specific FO_Order
                    FETCH FIRST 1 ROWS ONLY            -- Optimize for single record retrieval
                    WITH UR                           -- Uncommitted Read to avoid locks";

            cmd.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.Numeric, Value = foOrder });

            using var r = cmd.ExecuteReader();
            return r.Read() ? MapToEntity(r) : null;
        }, ct);
    }

    /// <summary>
    /// New: Fetches full shipping and address details for an order.
    /// </summary>
    public async Task<IReadOnlyList<ShippingAddressEntity>> GetShippingDetailAsync(long orderId, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var list = new List<ShippingAddressEntity>();
            using var cn = new OleDbConnection(_cs);
            cn.Open();
            using var cmd = cn.CreateCommand();

            cmd.CommandText = @"
            SELECT
                LO.FULFILL_ORDER_NUMBER,
                RA.SHP_TRK_ACTUAL_SHIP_DATE,
                NW.ITEM_CODE,
                LO.FO_STUDENT_FIRST_MIDDLE,
                LO.FO_STUDENT_LAST_NAME,
                SC.SCHOOL_OFFICIAL_NAME,
                AD.FO_ADDRESS_NAME,
                AD.FO_ADDRESS_LINE_1,
                AD.FO_ADDRESS_LINE_2,
                AD.FO_ADDRESS_LINE_3,
                AD.SCZ_STATE,
                AD.SZC_ZIP_CODE,
                AD.SZC_CITY_NAME,
                MIN(RA.SHP_TRK_NUMBER) AS SHP_TRK_NUMBER,
                MIN(RA.SHP_TRK_CUSTOMER_NUMBER) AS SHP_TRK_CUSTOMER_NUMBER
            FROM CLLOCPP AS LO
            LEFT JOIN CLRACPP AS RA ON LO.FULFILL_ORDER_NUMBER = RA.SHP_TRK_FO_GRP_ORD_NBR
            LEFT JOIN CLNYCPL2 AS AD ON AD.FULFILL_ORDER_NUMBER = LO.FULFILL_ORDER_NUMBER AND AD.FO_ADDRESS_TYPE = 'ST'
            LEFT JOIN CLNWCPP AS NW ON NW.FULFILL_ORDER_NUMBER = LO.FULFILL_ORDER_NUMBER
            LEFT JOIN CCPRDDTA.CLBUREP AS SC ON SC.SCHOOL_NUMBER = LO.SCHOOL_NUMBER AND SC.SCZ_STATE = AD.SCZ_STATE
            WHERE LO.FULFILL_ORDER_NUMBER = ?
            GROUP BY 
                LO.FULFILL_ORDER_NUMBER, RA.SHP_TRK_ACTUAL_SHIP_DATE, NW.ITEM_CODE,
                LO.FO_STUDENT_FIRST_MIDDLE, LO.FO_STUDENT_LAST_NAME, SC.SCHOOL_OFFICIAL_NAME,
                AD.FO_ADDRESS_TYPE, AD.FO_ADDRESS_NAME, AD.FO_ADDRESS_LINE_1, AD.FO_ADDRESS_LINE_2,
                AD.FO_ADDRESS_LINE_3, AD.SCZ_STATE, AD.SZC_ZIP_CODE, AD.SZC_CITY_NAME
            WITH UR";

            cmd.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.Numeric, Value = orderId });

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new ShippingAddressEntity
                {
                    FoOrder = Convert.ToDecimal(r["FULFILL_ORDER_NUMBER"]),
                    ShipDate = r.IsDBNull(r.GetOrdinal("SHP_TRK_ACTUAL_SHIP_DATE")) ? 0 : Convert.ToDecimal(r["SHP_TRK_ACTUAL_SHIP_DATE"]),
                    ItemCode = r["ITEM_CODE"].ToString()?.Trim() ?? "",
                    FirstName = r["FO_STUDENT_FIRST_MIDDLE"].ToString()?.Trim() ?? "",
                    LastName = r["FO_STUDENT_LAST_NAME"].ToString()?.Trim() ?? "",
                    SchoolName = r.IsDBNull(r.GetOrdinal("SCHOOL_OFFICIAL_NAME")) ? "" : r["SCHOOL_OFFICIAL_NAME"].ToString()!.Trim(),
                    AddressName = r["FO_ADDRESS_NAME"].ToString()?.Trim() ?? "",
                    AddressLine1 = r["FO_ADDRESS_LINE_1"].ToString()?.Trim() ?? "",
                    AddressLine2 = r["FO_ADDRESS_LINE_2"].ToString()?.Trim() ?? "",
                    AddressLine3 = r["FO_ADDRESS_LINE_3"].ToString()?.Trim() ?? "",
                    State = r["SCZ_STATE"].ToString()?.Trim() ?? "",
                    ZipCode = r["SZC_ZIP_CODE"].ToString()?.Trim() ?? "",
                    City = r["SZC_CITY_NAME"].ToString()?.Trim() ?? "",
                    TrackingNumber = r.IsDBNull(r.GetOrdinal("SHP_TRK_NUMBER")) ? "" : r["SHP_TRK_NUMBER"].ToString()!.Trim(),
                    CustomerTrackingNumber = r.IsDBNull(r.GetOrdinal("SHP_TRK_CUSTOMER_NUMBER")) ? "" : r["SHP_TRK_CUSTOMER_NUMBER"].ToString()!.Trim()
                });
            }
            return list;
        }, ct);
    }

    /// <summary>
    /// Maps an OleDbDataReader row to an AnnouncementEntity.
    /// Handles NULL values from LEFT JOINs.
    /// </summary>
    private static Announcement MapToEntity(OleDbDataReader r)
    {
        return new Announcement
        {
            // Usamos Convert.ToDecimal y Convert.ToInt32 para evitar el error de "Specified cast is not valid"
            FoOrder = Convert.ToDecimal(r.GetValue(r.GetOrdinal("FO_Order"))),
            OrdSts = r.GetString(r.GetOrdinal("Ord_Sts")).Trim(),
            MfgPlanner = r.GetString(r.GetOrdinal("Mfg_Planner")).Trim(),
            BookDt = Convert.ToDecimal(r.GetValue(r.GetOrdinal("Book_Dt"))),
            ShipDt = Convert.ToDecimal(r.GetValue(r.GetOrdinal("Ship_Dt"))),
            CancelDt = Convert.ToDecimal(r.GetValue(r.GetOrdinal("Cancel_Dt"))),
            ReturnDt = Convert.ToDecimal(r.GetValue(r.GetOrdinal("Return_Dt"))),
            Class = r.GetString(r.GetOrdinal("Class")).Trim(),

            XRef = r.IsDBNull(r.GetOrdinal("X_REF")) ? "" : r.GetValue(r.GetOrdinal("X_REF")).ToString()!.Trim(),
            Po = r.IsDBNull(r.GetOrdinal("PO")) ? "" : r.GetValue(r.GetOrdinal("PO")).ToString()!.Trim(),
            LastName = r.IsDBNull(r.GetOrdinal("Last_Name")) ? "" : r.GetValue(r.GetOrdinal("Last_Name")).ToString()!.Trim(),
            FirstName = r.IsDBNull(r.GetOrdinal("First_Name")) ? "" : r.GetValue(r.GetOrdinal("First_Name")).ToString()!.Trim(),

            OrderTotal = Convert.ToDecimal(r.GetValue(r.GetOrdinal("Order_Total"))),
            Payments = Convert.ToDecimal(r.GetValue(r.GetOrdinal("Payments"))),
            BalanceDue = Convert.ToDecimal(r.GetValue(r.GetOrdinal("Balance_Due"))),

            PaymentTerm = r.IsDBNull(r.GetOrdinal("Payment_Term")) ? "" : r.GetValue(r.GetOrdinal("Payment_Term")).ToString()!.Trim(),
            Surrogate = Convert.ToDecimal(r.GetValue(r.GetOrdinal("Surrogate"))),
            SchoolState = r.IsDBNull(r.GetOrdinal("School_State")) ? "" : r.GetValue(r.GetOrdinal("School_State")).ToString()!.Trim(),
            SchoolNum = r.IsDBNull(r.GetOrdinal("School_Num")) ? "" : r.GetValue(r.GetOrdinal("School_Num")).ToString()!.Trim(),
            SellingStore = r.IsDBNull(r.GetOrdinal("Selling_Store")) ? "" : r.GetValue(r.GetOrdinal("Selling_Store")).ToString()!.Trim(),
            SellingLn = Convert.ToDecimal(r.GetValue(r.GetOrdinal("Selling_LN"))),
            BillingStore = r.IsDBNull(r.GetOrdinal("Billing_Store")) ? "" : r.GetValue(r.GetOrdinal("Billing_Store")).ToString()!.Trim(),
            BillingLine = Convert.ToDecimal(r.GetValue(r.GetOrdinal("Billing_Line"))),
            LineSequence = Convert.ToDecimal(r.GetValue(r.GetOrdinal("Line_Sequnce"))),
            LineStatus = r.IsDBNull(r.GetOrdinal("Line_Status")) ? "" : r.GetValue(r.GetOrdinal("Line_Status")).ToString()!.Trim(),
            StatusDt = Convert.ToDecimal(r.GetValue(r.GetOrdinal("Status_Dt"))),

            ItemCd = r.IsDBNull(r.GetOrdinal("Item_Cd")) ? "" : r.GetValue(r.GetOrdinal("Item_Cd")).ToString()!.Trim(),
            MetalId = r.IsDBNull(r.GetOrdinal("Metal_Id")) ? "" : r.GetValue(r.GetOrdinal("Metal_Id")).ToString()!.Trim(),
            PmClass = r.IsDBNull(r.GetOrdinal("PM_Class")) ? "" : r.GetValue(r.GetOrdinal("PM_Class")).ToString()!.Trim(),
            ProdLine = r.IsDBNull(r.GetOrdinal("Prod_Line")) ? "" : r.GetValue(r.GetOrdinal("Prod_Line")).ToString()!.Trim(),

            GroupShip = Convert.ToDecimal(r.GetValue(r.GetOrdinal("Group_Ship"))),
            GroupShipSeq = Convert.ToDecimal(r.GetValue(r.GetOrdinal("Group_Ship_Seq"))),

            // Campos del LEFT JOIN
            ShipTrackingNum = r.IsDBNull(r.GetOrdinal("Ship_Tracking_Num")) ? "" : r.GetValue(r.GetOrdinal("Ship_Tracking_Num")).ToString()!.Trim(),
            ShippingMthd = r.IsDBNull(r.GetOrdinal("Shipping_MTHD")) ? "" : r.GetValue(r.GetOrdinal("Shipping_MTHD")).ToString()!.Trim(),
            ShippingDt = r.IsDBNull(r.GetOrdinal("Shipping_Dt")) ? 0 : Convert.ToDecimal(r.GetValue(r.GetOrdinal("Shipping_Dt"))),
            DeliveryDt = r.IsDBNull(r.GetOrdinal("Delivery_Dt")) ? 0 : Convert.ToDecimal(r.GetValue(r.GetOrdinal("Delivery_Dt")))
        };
    }
      
}
