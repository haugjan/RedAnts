using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CatalogWorkflow;

public static class SetEventFreeEntryQuotas
{
    public sealed record Command(int EventId, IReadOnlyDictionary<FreeEntryType, int?> Quotas, IReadOnlyDictionary<FreeEntryType, int?> FixedCounts);

    public sealed class Handler(IFreeEntryRepository freeEntries)
    {
        public Task HandleAsync(Command command) =>
            freeEntries.SaveQuotaAsync(command.EventId, FreeEntryQuota.Create(command.Quotas, command.FixedCounts));
    }
}
