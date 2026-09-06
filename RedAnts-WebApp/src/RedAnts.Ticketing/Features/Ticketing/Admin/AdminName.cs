namespace RedAnts.Features.Ticketing.Admin;

public static class AdminName
{
    public static string LastFirst(string? first, string? last)
    {
        var l = (last ?? "").Trim();
        var f = (first ?? "").Trim();
        return string.Join(" ", new[] { l, f }.Where(s => s.Length > 0));
    }

    public static string? Display(string? company, string? first, string? last)
    {
        var c = (company ?? "").Trim();
        var person = LastFirst(first, last);
        if (c.Length > 0) return person.Length > 0 ? $"{c} · {person}" : c;
        return person.Length > 0 ? person : null;
    }

    public static string SortKey(string? company, string? first, string? last)
    {
        var l = (last ?? "").Trim();
        if (l.Length > 0) return (l + " " + (first ?? "").Trim()).Trim();
        return (company ?? "").Trim();
    }
}
