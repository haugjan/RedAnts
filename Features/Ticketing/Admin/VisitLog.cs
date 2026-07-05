using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

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

public sealed record TicketVisitScan(VisitLogType Type, DateTime OccurredAt, string? ScannedBy);

public sealed record TicketVisitEntry(
    long VisitId,
    int EventId,
    string EventName,
    DateOnly? EventDate,
    bool IsInside,
    IReadOnlyList<TicketVisitScan> Scans);

public interface IVisitLogReader
{
    Task<IReadOnlyList<TicketVisitEntry>> GetByTicketUuidAsync(Guid uuid);

    Task<IReadOnlyDictionary<Guid, bool>> GetInsideByEventAsync(int eventId);
}
