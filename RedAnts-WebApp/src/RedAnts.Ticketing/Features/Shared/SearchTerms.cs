namespace RedAnts.Ticketing.Features.Shared;

public static class SearchTerms
{
    public static string[] Parse(string? search) =>
        (search ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static bool Matches(string haystack, string[] terms) =>
        terms.All(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
}
