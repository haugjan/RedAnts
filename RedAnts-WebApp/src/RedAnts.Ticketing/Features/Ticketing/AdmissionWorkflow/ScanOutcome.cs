using RedAnts.Domain.Ticketing.Admission;
using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.AdmissionWorkflow;

public enum AdmissionOutcome
{
    CheckedIn,
    CheckedOut,
    Rejected,
    Test
}

public sealed record PriorScan(DateTimeOffset At, string? By);

public sealed record ScanOutcome(
    AdmissionOutcome Outcome,
    TicketType? Type,
    string? Reference,
    string? Reason,
    Occupancy Occupancy,
    string? CategoryLabel = null,
    string? Holder = null,
    DateTimeOffset? PriorAt = null,
    string? PriorBy = null,
    int? AdmissionsUsed = null,
    int? AdmissionCap = null,
    IReadOnlyList<PriorScan>? Priors = null)
{
    public bool Ok => Outcome != AdmissionOutcome.Rejected;
}
