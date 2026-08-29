using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace RedAnts.Features.Ticketing.Public;

public static class DisplayCulture
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-CH");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-GB");

    public static CultureInfo From(HttpRequest request)
    {
        var preferred = request.GetTypedHeaders().AcceptLanguage
            .OrderByDescending(l => l.Quality ?? 1)
            .FirstOrDefault()?.Value.Value;
        return preferred is not null && preferred.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? English
            : German;
    }
}
