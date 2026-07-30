using System.Globalization;

namespace Service.Ann.Batch.Api.Common.Helpers;

/// <summary>
/// Utility class to handle date conversions between AS400 (CYYMMDD) and ISO 8601 (YYYY-MM-DD) formats.
/// </summary>
public static class As400DateHelper
{
    /// <summary>
    /// Converts an ISO date string (YYYY-MM-DD) to an AS400 decimal format (CYYMMDD).
    /// </summary>
    /// <param name="isoDate">The date string in YYYY-MM-DD format.</param>
    /// <returns>A decimal representing the date in CYYMMDD format (Century: 1 for 2000s, 0 for 1900s).</returns>
    /// <example>2026-05-11 -> 1260511</example>
    public static decimal ToAs400Date(string? isoDate)
    {
        if (string.IsNullOrWhiteSpace(isoDate) ||
            !DateTime.TryParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            return 0;
        }

        // Century indicator: 1 for years 2000 and above, 0 for 1900-1999
        string century = date.Year >= 2000 ? "1" : "0";
        string formatted = $"{century}{date:yyMMdd}";

        return decimal.Parse(formatted);
    }

    /// <summary>
    /// Converts an AS400 decimal date (CYYMMDD) to an ISO 8601 string (YYYY-MM-DD).
    /// </summary>
    /// <param name="as400Date">The numeric date from AS400.</param>
    /// <returns>A formatted string (YYYY-MM-DD) or an empty string if the date is invalid.</returns>
    /// <example>1260511 -> 2026-05-11</example>
    public static string Format(decimal as400Date)
    {
        if (as400Date <= 0) return string.Empty;

        // Convert to string and pad with leading zeros to ensure it has 7 characters (CYYMMDD)
        // This is crucial for 1900s dates where the century digit '0' might be dropped as a numeric value.
        string dateStr = as400Date.ToString(CultureInfo.InvariantCulture).PadLeft(7, '0');

        try
        {
            string centuryIndicator = dateStr.Substring(0, 1);
            string yearPart = dateStr.Substring(1, 2);
            string monthPart = dateStr.Substring(3, 2);
            string dayPart = dateStr.Substring(5, 2);

            string fullYear = centuryIndicator == "1" ? $"20{yearPart}" : $"19{yearPart}";

            return $"{fullYear}-{monthPart}-{dayPart}";
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Converts an AS400 decimal date (CYYMMDD) to a nullable DateOnly object.
    /// </summary>
    /// <param name="as400Date">The numeric date from AS400.</param>
    /// <returns>A DateOnly object or null if the date is zero or invalid.</returns>
    public static DateOnly? ToDateOnly(decimal as400Date)
    {
        var isoString = Format(as400Date);

        if (string.IsNullOrEmpty(isoString) ||
            !DateOnly.TryParse(isoString, out DateOnly result))
        {
            return null;
        }

        return result;
    }
}