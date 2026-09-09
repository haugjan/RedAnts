using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admission;

namespace RedAnts.Ticketing.Features.Catalog;

public sealed record EventFreeEntryQuotas(IReadOnlyDictionary<FreeEntryType, int?> Quotas, IReadOnlyDictionary<FreeEntryType, int?> FixedCounts);

public static class GetEventFreeEntryQuotas
{
    public sealed record Query(int EventId);

    public sealed class Handler(IFreeEntryRepository freeEntries)
    {
        public async Task<EventFreeEntryQuotas> HandleAsync(Query query)
        {
            var quota = await freeEntries.GetQuotaAsync(query.EventId);
            var types = Enum.GetValues<FreeEntryType>();
            return new EventFreeEntryQuotas(
                types.ToDictionary(t => t, t => quota.QuotaFor(t)),
                types.ToDictionary(t => t, t => quota.FixedFor(t) is var fixedCount && fixedCount > 0 ? fixedCount : (int?)null));
        }
    }
}
