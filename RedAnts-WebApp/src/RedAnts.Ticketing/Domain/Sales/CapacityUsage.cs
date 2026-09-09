namespace RedAnts.Ticketing.Domain.Sales;

public sealed record CapacityUsage(int SoldTotal, IReadOnlyDictionary<int, int> SoldByTier)
{
    public static CapacityUsage None { get; } = new(0, new Dictionary<int, int>());

    public int SoldFor(int tierId) => SoldByTier.TryGetValue(tierId, out var sold) ? sold : 0;
}

public static class CapacityDenied
{
    public sealed record TotalExhausted() : CheckResult.Denied.Reason("Nicht mehr genügend Plätze verfügbar.");

    public sealed record TierUnavailable(int TierId) : CheckResult.Denied.Reason("Eine gewählte Preisstufe ist nicht mehr verfügbar.");

    public sealed record TierExhausted(int TierId, int Remaining) : CheckResult.Denied.Reason("Eine gewählte Preisstufe ist nicht mehr in dieser Anzahl verfügbar.");

    public sealed record Contended() : CheckResult.Denied.Reason("Das Kontingent wurde soeben verändert. Bitte versuche es noch einmal.");
}
