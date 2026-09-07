using RedAnts.Domain.Ticketing.Admission;
using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.AdmissionWorkflow;

public static class ScanTicket
{
    public sealed record Command(int EventId, TicketType Type, Guid Uuid, int ScopeId, ScanMode Mode, string? ScannedBy, bool Test = false);

    public sealed class Handler(TicketScanning scanning)
    {
        public Task<ScanOutcome> HandleAsync(Command command) =>
            scanning.ScanAsync(command.EventId, command.Type, command.Uuid, command.ScopeId, command.Mode, command.ScannedBy, command.Test);
    }
}
