using System.Globalization;

namespace PersonalExpenseTracker.Services;

public static class CurrencyCulture
{
    private static readonly IReadOnlyDictionary<string, string> CultureNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = "en-US",
            ["EUR"] = "de-DE",
            ["VND"] = "vi-VN",
            ["GBP"] = "en-GB",
            ["JPY"] = "ja-JP",
            ["AUD"] = "en-AU",
            ["CAD"] = "en-CA"
        };

    public static NumberFormatInfo GetNumberFormat(string? currencyCode)
    {
        var cultureName = currencyCode is not null && CultureNames.TryGetValue(currencyCode, out var mapped)
            ? mapped
            : CultureNames["USD"];
        return (NumberFormatInfo)CultureInfo.GetCultureInfo(cultureName).NumberFormat.Clone();
    }

    public static void ApplyCurrency(string? currencyCode)
    {
        var requestCulture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
        requestCulture.NumberFormat = GetNumberFormat(currencyCode);
        CultureInfo.CurrentCulture = requestCulture;
    }
}
