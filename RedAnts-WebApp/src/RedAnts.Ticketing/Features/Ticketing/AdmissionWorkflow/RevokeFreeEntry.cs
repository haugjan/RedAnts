using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.AdmissionWorkflow;

public static class RevokeFreeEntry
{
    public sealed record Command(int EventId, FreeEntryType Type, string? ScannedBy);

    public sealed class Handler(IFreeEntryRepository freeEntries, IOccupancyReader occupancy)
    {
        public async Task<ScanOutcome> HandleAsync(Command command)
        {
            var entry = await freeEntries.FindLatestInsideAsync(command.EventId, command.Type);
            if (entry is null)
                return new ScanOutcome(AdmissionOutcome.Rejected, TicketType.FreeEntry, command.Type.DisplayName(),
                    $"Kein freier Einlass ({command.Type.DisplayName()}) zum Auschecken.", await occupancy.GetAsync(command.EventId));

            entry.Revoke(command.ScannedBy, SwissTime.Timestamp);
            await freeEntries.SaveAsync(entry);
            return new ScanOutcome(AdmissionOutcome.CheckedOut, TicketType.FreeEntry, command.Type.DisplayName(), null,
                await occupancy.GetAsync(command.EventId));
        }
    }
}
