using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Admission.Admin;

public enum RedemptionState
{
    Open,
    Redeemed,
    Outside
}

public static class RedemptionStateExtensions
{
    public static string DisplayName(this RedemptionState state) => state switch
    {
        RedemptionState.Open => "Offen",
        RedemptionState.Redeemed => "Eingelöst",
        RedemptionState.Outside => "Draussen",
        _ => state.ToString()
    };

    public static RedemptionState Derive(bool redeemed, bool? isInside) =>
        !redeemed ? RedemptionState.Open
        : isInside == false ? RedemptionState.Outside
        : RedemptionState.Redeemed;
}

public sealed record TicketVisitScan(VisitLogType Type, DateTimeOffset OccurredAt, string? ScannedBy);

public enum TicketVisitKind { Visit, Conversion }

public sealed record TicketVisitEntry(
    long VisitId,
    int EventId,
    string EventName,
    DateOnly? EventDate,
    bool IsInside,
    IReadOnlyList<TicketVisitScan> Scans,
    TicketVisitKind Kind = TicketVisitKind.Visit,
    DateTimeOffset? ConvertedAt = null,
    bool ViaConversion = false,
    Guid? TicketUuid = null);
