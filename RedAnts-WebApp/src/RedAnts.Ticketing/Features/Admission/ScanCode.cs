using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features.Admission;

public static class ScanCode
{
    public const string BadCode = "Der Code besteht aus den ersten 8 Zeichen der Ticket-Nr.";
    public const string UnknownCode = "Kein Ticket mit diesem Code gefunden.";

    public sealed record Command(int EventId, string ShortCode, ScanMode Mode, string? ScannedBy, bool Test = false);

    public sealed class Handler(IIssuedTicketReader tickets, IOccupancyReader occupancy, TicketScanning scanning)
    {
        public async Task<ScanOutcome> HandleAsync(Command command)
        {
            var code = (command.ShortCode ?? "").Trim().Replace(" ", "").ToLowerInvariant();
            if (code.Length != 8 || !code.All(Uri.IsHexDigit))
                return await RejectAsync(command.EventId, command.ShortCode, BadCode);

            var issued = await tickets.FindByCodeAsync(code);
            if (issued is null)
                return await RejectAsync(command.EventId, code, UnknownCode);

            return await scanning.ScanAsync(command.EventId, issued.Type, issued.Uuid, issued.ScopeId, command.Mode, command.ScannedBy, command.Test);
        }

        private async Task<ScanOutcome> RejectAsync(int eventId, string? reference, string reason) =>
            new(AdmissionOutcome.Rejected, null, reference?.Trim().ToUpperInvariant(), reason, await occupancy.GetAsync(eventId));
    }
}
