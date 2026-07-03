using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Scanning;

/// <summary>What a scan/free-entry action did at the door.</summary>
public enum AdmissionOutcome { CheckedIn, CheckedOut, Rejected }

/// <summary>Live occupancy of an event's hall: how many people are currently inside and the admission
/// quota (Einlasskontingent, null = unlimited).</summary>
public sealed record Occupancy(int Inside, int? Quota)
{
    /// <summary>Free places left, or null when there is no quota.</summary>
    public int? Remaining => Quota is { } q ? Math.Max(0, q - Inside) : null;
    public bool Full => Quota is { } q && Inside >= q;
}

/// <summary>The result of a door action, with the updated occupancy so the scanner can show how many
/// people may still be admitted.</summary>
public sealed record ScanOutcome(
    AdmissionOutcome Outcome,
    TicketType? Type,
    string? Reference,
    string? Reason,
    Occupancy Occupancy)
{
    public bool Ok => Outcome != AdmissionOutcome.Rejected;
}

/// <summary>Door admission for one event: toggles a ticket's presence (check in / check out on re-scan),
/// grants and revokes anonymous free admissions (counted against the hall quota), and reports the live
/// occupancy. Persists to the visits/visit-log tables; the hall quota comes from the event price set.</summary>
public interface IAdmissionService
{
    Task<Occupancy> GetOccupancyAsync(int eventId);

    /// <summary>Toggle a scanned ticket's presence at this event: admit it if it is outside, or check it
    /// out if it is already inside. Rejects unknown/cancelled tickets and tickets for another event/season.
    /// Ticketed admissions are never blocked by the quota (they are sold); the quota is informational.</summary>
    Task<ScanOutcome> ScanTicketAsync(int eventId, TicketType type, Guid uuid, int scopeId, string? scannedBy);

    /// <summary>Admit one anonymous free person (Freier Einlass). Counts against the hall quota and is
    /// rejected once the hall is full.</summary>
    Task<ScanOutcome> GrantFreeEntryAsync(int eventId, string? scannedBy);

    /// <summary>Check out one currently-inside free admission (most recent first).</summary>
    Task<ScanOutcome> RevokeFreeEntryAsync(int eventId, string? scannedBy);
}
