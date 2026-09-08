using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Domain.Ticketing.Admission;

public static class FreeEntryDenied
{
    public sealed record HallFull(FreeEntryType Type)
        : CheckResult.Denied.Reason($"Halle voll — kein Gratiseintritt ({Type.DisplayName()}) mehr.");

    public sealed record QuotaExhausted(FreeEntryType Type, int Used, int Quota)
        : CheckResult.Denied.Reason($"Kontingent für {Type.DisplayName()} erschöpft ({Used}/{Quota}).");
}

public sealed record FreeEntryQuota(IReadOnlyDictionary<FreeEntryType, int?> Quotas, IReadOnlyDictionary<FreeEntryType, int> Fixed)
{
    public static FreeEntryQuota Unlimited { get; } = new(new Dictionary<FreeEntryType, int?>(), new Dictionary<FreeEntryType, int>());

    public static FreeEntryQuota Create(IReadOnlyDictionary<FreeEntryType, int?> quotas, IReadOnlyDictionary<FreeEntryType, int?> fixedCounts)
    {
        if (quotas.Values.Any(q => q is < 0) || fixedCounts.Values.Any(f => f is < 0))
            throw new DomainException("Kontingente und feste Anzahlen dürfen nicht negativ sein.");
        return new FreeEntryQuota(
            quotas.ToDictionary(p => p.Key, p => p.Value),
            fixedCounts.Where(p => p.Value is { } f && f > 0).ToDictionary(p => p.Key, p => p.Value!.Value));
    }

    public int? QuotaFor(FreeEntryType type) => Quotas.TryGetValue(type, out var quota) ? quota : null;

    public int FixedFor(FreeEntryType type) => Fixed.TryGetValue(type, out var count) ? count : 0;

    public int FixedTotal => Fixed.Values.Sum();

    public CheckResult Allows(FreeEntryType type, int granted, Occupancy occupancy)
    {
        if (occupancy.Full && type is FreeEntryType.SwissUnihockeyFreeCard or FreeEntryType.Child)
            return CheckResult.Deny(new FreeEntryDenied.HallFull(type));
        var used = granted + FixedFor(type);
        if (QuotaFor(type) is { } quota && used >= quota)
            return CheckResult.Deny(new FreeEntryDenied.QuotaExhausted(type, used, quota));
        return CheckResult.Allow();
    }
}

public sealed class FreeEntry
{
    private readonly List<VisitLog> _logs;
    private readonly List<VisitLog> _newLogs = [];

    public long VisitId { get; private set; }
    public int EventId { get; }
    public FreeEntryType Type { get; }
    public Guid Uuid { get; }
    public bool IsInside { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    private FreeEntry(long visitId, int eventId, FreeEntryType type, Guid uuid, bool isInside, DateTimeOffset createdAt, List<VisitLog> logs)
    {
        VisitId = visitId;
        EventId = eventId;
        Type = type;
        Uuid = uuid;
        IsInside = isInside;
        CreatedAt = createdAt;
        _logs = logs;
    }

    public static FreeEntry Grant(int eventId, FreeEntryType type, string? scannedBy, DateTimeOffset now)
    {
        if (eventId <= 0) throw new DomainException("Ein Anlass muss zugewiesen sein.");
        var entry = new FreeEntry(0, eventId, type, Guid.NewGuid(), true, now, []);
        entry.AddLog(VisitLogType.CheckIn, now, scannedBy);
        return entry;
    }

    public static FreeEntry FromPersistence(long visitId, int eventId, FreeEntryType type, Guid uuid, bool isInside, DateTimeOffset createdAt,
        IEnumerable<VisitLog> logs) =>
        new(visitId, eventId, type, uuid, isInside, createdAt, logs.OrderBy(l => l.OccurredAt).ThenBy(l => l.Id).ToList());

    public bool IsNew => VisitId == 0;
    public IReadOnlyList<VisitLog> Logs => _logs;
    public IReadOnlyList<VisitLog> NewLogs => _newLogs;

    public void Revoke(string? scannedBy, DateTimeOffset now)
    {
        if (!IsInside) throw new DomainException($"Kein freier Einlass ({Type.DisplayName()}) zum Auschecken.");
        IsInside = false;
        AddLog(VisitLogType.CheckOut, now, scannedBy);
    }

    public void MarkPersisted(long visitId)
    {
        VisitId = visitId;
        _newLogs.Clear();
    }

    private void AddLog(VisitLogType type, DateTimeOffset now, string? scannedBy)
    {
        var log = new VisitLog(0, type, now, scannedBy);
        _logs.Add(log);
        _newLogs.Add(log);
    }
}
