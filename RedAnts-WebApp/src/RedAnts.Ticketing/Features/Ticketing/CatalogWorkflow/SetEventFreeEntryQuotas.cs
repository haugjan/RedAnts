using RedAnts.Domain.Ticketing.Admission;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CatalogWorkflow;

public static class SetEventFreeEntryQuotas
{
    public sealed record Command(int EventId, IReadOnlyDictionary<FreeEntryType, int?> Quotas, IReadOnlyDictionary<FreeEntryType, int?> FixedCounts);

    public sealed class Handler(IFreeEntryRepository freeEntries)
    {
        public Task HandleAsync(Command command) =>
            freeEntries.SaveQuotaAsync(command.EventId, FreeEntryQuota.Create(command.Quotas, command.FixedCounts));
    }
}
