using System.Globalization;

namespace RedAnts.Features.Ticketing;

public static class MoneyFormat
{
    public static string Amount(decimal value)
    {
        var rounded = decimal.Round(value, 2);
        return rounded == Math.Truncate(rounded)
            ? rounded.ToString("N0", CultureInfo.CurrentCulture)
            : rounded.ToString("N2", CultureInfo.CurrentCulture);
    }
}
