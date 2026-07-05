using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Scanning;

public enum AdmissionOutcome { CheckedIn, CheckedOut, Rejected }

public sealed record Occupancy(int Inside, int? Quota)
{
    public int? Remaining => Quota is { } q ? Math.Max(0, q - Inside) : null;
    public bool Full => Quota is { } q && Inside >= q;
}

public sealed record ScanOutcome(
    AdmissionOutcome Outcome,
    TicketType? Type,
    string? Reference,
    string? Reason,
    Occupancy Occupancy)
{
    public bool Ok => Outcome != AdmissionOutcome.Rejected;
}

public interface IAdmissionService
{
    Task<Occupancy> GetOccupancyAsync(int eventId);

    Task<ScanOutcome> ScanTicketAsync(int eventId, TicketType type, Guid uuid, int scopeId, string? scannedBy);

    Task<ScanOutcome> GrantFreeEntryAsync(int eventId, string? scannedBy);

    Task<ScanOutcome> RevokeFreeEntryAsync(int eventId, string? scannedBy);
}
