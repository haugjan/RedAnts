using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Admission;

public static class GrantFreeEntry
{
    public sealed record Command(int EventId, FreeEntryType Type, string? ScannedBy);

    public sealed class Handler(IFreeEntryRepository freeEntries, IOccupancyReader occupancy)
    {
        public async Task<ScanOutcome> HandleAsync(Command command)
        {
            var before = await occupancy.GetAsync(command.EventId);
            var quota = await freeEntries.GetQuotaAsync(command.EventId);
            var granted = await freeEntries.CountGrantedAsync(command.EventId, command.Type);
            if (quota.Allows(command.Type, granted, before) is CheckResult.Denied denied)
                return new ScanOutcome(AdmissionOutcome.Rejected, TicketType.FreeEntry, null, denied.Cause.Message, before);

            var entry = FreeEntry.Grant(command.EventId, command.Type, command.ScannedBy, SwissTime.Timestamp);
            await freeEntries.SaveAsync(entry);
            return new ScanOutcome(AdmissionOutcome.CheckedIn, TicketType.FreeEntry, command.Type.DisplayName(), null,
                await occupancy.GetAsync(command.EventId));
        }
    }
}
