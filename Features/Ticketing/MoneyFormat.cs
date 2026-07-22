using System.Globalization;

namespace RedAnts.Features.Ticketing;

public static class MoneyFormat
{
    private static readonly NumberFormatInfo Swiss = new()
    {
        NumberDecimalSeparator = ".",
        NumberGroupSeparator = "’",
        NumberGroupSizes = [3]
    };

    public static string Amount(decimal value)
    {
        var rounded = decimal.Round(value, 2);
        return rounded == Math.Truncate(rounded)
            ? rounded.ToString("N0", Swiss)
            : rounded.ToString("N2", Swiss);
    }

    public static string Chf(decimal value) => decimal.Round(value, 2).ToString("N2", Swiss);
}
