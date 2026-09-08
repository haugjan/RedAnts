using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Domain.Ticketing.Admission;

public sealed record VisitLog(long Id, VisitLogType Type, DateTimeOffset OccurredAt, string? ScannedBy);

public sealed class AdmissionVisit
{
    private readonly List<VisitLog> _logs;
    private readonly List<VisitLog> _newLogs = [];

    public long Id { get; private set; }
    public bool IsInside { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public int? OriginType { get; }
    public string? OriginCardUuid { get; }
    public bool Changed { get; private set; }

    private AdmissionVisit(long id, bool isInside, DateTimeOffset createdAt, int? originType, string? originCardUuid, List<VisitLog> logs)
    {
        Id = id;
        IsInside = isInside;
        CreatedAt = createdAt;
        OriginType = originType;
        OriginCardUuid = originCardUuid;
        _logs = logs;
    }

    public static AdmissionVisit FromPersistence(long id, bool isInside, DateTimeOffset createdAt, int? originType, string? originCardUuid,
        IEnumerable<VisitLog> logs) =>
        new(id, isInside, createdAt, originType, originCardUuid, logs.OrderBy(l => l.OccurredAt).ThenBy(l => l.Id).ToList());

    internal static AdmissionVisit Start(DateTimeOffset now, string? scannedBy, int? originType, string? originCardUuid)
    {
        var visit = new AdmissionVisit(0, true, now, originType, originCardUuid, []) { Changed = true };
        visit.AddLog(VisitLogType.CheckIn, now, scannedBy);
        return visit;
    }

    public bool IsNew => Id == 0;
    public IReadOnlyList<VisitLog> Logs => _logs;
    public IReadOnlyList<VisitLog> NewLogs => _newLogs;
    public IEnumerable<VisitLog> CheckIns => _logs.Where(l => l.Type == VisitLogType.CheckIn);

    internal void Enter(DateTimeOffset now, string? scannedBy)
    {
        IsInside = true;
        Changed = true;
        AddLog(VisitLogType.CheckIn, now, scannedBy);
    }

    internal void Leave(DateTimeOffset now, string? scannedBy)
    {
        IsInside = false;
        Changed = true;
        AddLog(VisitLogType.CheckOut, now, scannedBy);
    }

    public void MarkPersisted(long id)
    {
        Id = id;
        Changed = false;
        _newLogs.Clear();
    }

    private void AddLog(VisitLogType type, DateTimeOffset now, string? scannedBy)
    {
        var log = new VisitLog(0, type, now, scannedBy);
        _logs.Add(log);
        _newLogs.Add(log);
    }
}

public sealed class Admission
{
    private readonly List<AdmissionVisit> _visits;

    public int EventId { get; }
    public TicketType TicketType { get; }
    public Guid TicketUuid { get; }

    private Admission(int eventId, TicketType ticketType, Guid ticketUuid, List<AdmissionVisit> visits)
    {
        EventId = eventId;
        TicketType = ticketType;
        TicketUuid = ticketUuid;
        _visits = visits;
    }

    public static Admission None(int eventId, TicketType ticketType, Guid ticketUuid)
    {
        if (eventId <= 0) throw new DomainException("Ein Anlass muss zugewiesen sein.");
        if (ticketType == TicketType.FreeEntry) throw new DomainException("Free-Entry hat kein Ticket.");
        return new Admission(eventId, ticketType, ticketUuid, []);
    }

    public static Admission FromPersistence(int eventId, TicketType ticketType, Guid ticketUuid, IEnumerable<AdmissionVisit> visits) =>
        new(eventId, ticketType, ticketUuid, visits.OrderBy(v => v.Id).ToList());

    public IReadOnlyList<AdmissionVisit> Visits => _visits;
    public int InsideCount => _visits.Count(v => v.IsInside);
    public bool MultiAdmission => TicketType == TicketType.MemberCard;
    public IReadOnlyList<VisitLog> CheckIns => _visits.SelectMany(v => v.CheckIns).OrderBy(l => l.OccurredAt).ToList();
    public VisitLog? LastCheckIn => CheckIns.LastOrDefault();

    public AdmissionVisit CheckIn(int admissionCap, string? scannedBy, DateTimeOffset now, int? originType = null, string? originCardUuid = null)
    {
        var cap = Math.Max(1, admissionCap);
        if (InsideCount >= cap)
            throw new DomainException(cap > 1 ? AdmissionEvaluator.AllAdmissionsUsed : AdmissionEvaluator.AlreadyCheckedIn);

        if (!MultiAdmission && _visits.FirstOrDefault() is { } existing)
        {
            existing.Enter(now, scannedBy);
            return existing;
        }

        var visit = AdmissionVisit.Start(now, scannedBy, originType, originCardUuid);
        _visits.Add(visit);
        return visit;
    }

    public AdmissionVisit CheckOut(string? scannedBy, DateTimeOffset now)
    {
        var inside = _visits.LastOrDefault(v => v.IsInside)
            ?? throw new DomainException(AdmissionEvaluator.NotCheckedIn);
        inside.Leave(now, scannedBy);
        return inside;
    }
}
