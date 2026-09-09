using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Domain.Admission;

public enum ScanMode
{
    CheckIn,
    CheckOut
}

public sealed record FreeEntryTally(FreeEntryType Type, int Inside, int Out);

public sealed record Occupancy(int Inside, int? Quota, int FreeInside = 0, IReadOnlyList<FreeEntryTally>? FreeTallies = null)
{
    public int? Remaining => Quota is { } q ? Math.Max(0, q - Inside) : null;
    public bool Full => Quota is { } q && Inside >= q;
    public IReadOnlyList<FreeEntryTally> Tallies => FreeTallies ?? [];

    public FreeEntryTally TallyFor(FreeEntryType type) =>
        Tallies.FirstOrDefault(t => t.Type == type) ?? new FreeEntryTally(type, 0, 0);
}
