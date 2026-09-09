namespace RedAnts.Ticketing.Domain.Sales;

public sealed record SeasonAddOnSet(int SeasonId, IReadOnlyList<SeasonAddOn> AddOns)
{
    public IEnumerable<SeasonAddOn> Active => AddOns.Where(a => a.Active);
}
