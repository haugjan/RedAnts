namespace RedAnts.Features.Ticketing.Admin;

public static class AdminFormat
{
    public static string TicketNo(Guid uuid) =>
        uuid == Guid.Empty ? "—" : uuid.ToString("N")[..8].ToUpperInvariant();

    public static string Initials(string? name)
    {
        var cleaned = name ?? "";
        var at = cleaned.IndexOf('@');
        if (at >= 0) cleaned = cleaned[..at];
        var parts = cleaned.Split(new[] { ' ', '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant(),
            _ => $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}"
        };
    }

    public static string CreatedShort(DateTime createdAtUtc, string? createdByName, int? orderId)
    {
        var date = createdAtUtc.ToLocalTime().ToString("dd.MM.yy");
        var by = orderId.HasValue ? "online" : createdByName is null ? "—" : Initials(createdByName);
        return $"{date} / {by}";
    }

    public static string CreatedTooltip(DateTime createdAtUtc, string? createdByName, string? createdByEmail, int? orderId)
    {
        var stamp = createdAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        if (orderId.HasValue) return $"{stamp} — Onlinekauf";
        var who = createdByEmail ?? createdByName;
        return who is null ? stamp : $"{stamp} — {who}";
    }
}
