using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Scanning;

public enum AdmissionOutcome { CheckedIn, CheckedOut, Rejected }

public enum ScanMode { CheckIn, CheckOut }

public sealed record Occupancy(int Inside, int? Quota, int FreeInside = 0)
{
    public int? Remaining => Quota is { } q ? Math.Max(0, q - Inside) : null;
    public bool Full => Quota is { } q && Inside >= q;
}

public sealed record ScanOutcome(
    AdmissionOutcome Outcome,
    TicketType? Type,
    string? Reference,
    string? Reason,
    Occupancy Occupancy,
    string? CategoryLabel = null,
    string? Holder = null,
    DateTime? PriorAt = null,
    string? PriorBy = null)
{
    public bool Ok => Outcome != AdmissionOutcome.Rejected;
}

public interface IAdmissionService
{
    Task<Occupancy> GetOccupancyAsync(int eventId);

    Task<ScanOutcome> ScanTicketAsync(int eventId, TicketType type, Guid uuid, int scopeId, ScanMode mode, string? scannedBy);

    Task<ScanOutcome> ScanCodeAsync(int eventId, string shortCode, ScanMode mode, string? scannedBy);

    Task<ScanOutcome> GrantFreeEntryAsync(int eventId, string? scannedBy);

    Task<ScanOutcome> RevokeFreeEntryAsync(int eventId, string? scannedBy);
}
