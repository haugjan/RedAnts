using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Domain.Admission;

public enum AdmissionVerdict
{
    TestEmpty,
    TestTicket,
    Admit,
    Reject
}

public sealed record ScannedTicketFacts(TicketType Type, int ScopeId, TicketStatus Status);

public sealed record AdmissionEvaluation(AdmissionVerdict Verdict, string? Reason = null);

public sealed record ScanContext(
    int EventId,
    TicketType RequestedType,
    int RequestedScopeId,
    ScanMode Mode,
    bool Test,
    bool IsEmptyUuid,
    ScannedTicketFacts? Ticket,
    int? EventSeasonId,
    int? RedeemedEventId,
    int AdmissionsInside,
    int AdmissionCap,
    bool RequiresConversion,
    bool IsBoxOfficeFlex);

public static class AdmissionEvaluator
{
    public const string UnknownTicket = "Unbekanntes Ticket.";
    public const string RecordMismatch = "Ticket stimmt nicht mit dem Datensatz überein.";
    public const string Blocked = "Ticket ist gesperrt.";
    public const string Cancelled = "Ticket ist storniert.";
    public const string WrongEvent = "Ticket gilt für einen anderen Anlass.";
    public const string UnknownEvent = "Anlass unbekannt.";
    public const string WrongSeason = "Ticket gilt für eine andere Saison.";
    public const string FlexRedeemedElsewhere = "Flexticket wurde bereits an einem anderen Anlass eingelöst.";
    public const string ConversionRequired = "Ticketumwandlung nötig — bitte auf der Event-Seite ein Ticket lösen.";
    public const string AlreadyCheckedIn = "Bereits eingecheckt.";
    public const string AllAdmissionsUsed = "Alle Einlässe dieser Karte bereits gebraucht.";
    public const string NotCheckedIn = "Noch nicht eingecheckt.";

    public static bool CarriesHolder(string? reason) =>
        reason is AlreadyCheckedIn or AllAdmissionsUsed or NotCheckedIn;

    public static AdmissionEvaluation Evaluate(ScanContext context)
    {
        if (context.IsEmptyUuid)
            return new AdmissionEvaluation(AdmissionVerdict.TestEmpty);

        var ticket = context.Ticket;
        if (ticket is null)
            return Reject(UnknownTicket);

        if (ticket.Type != context.RequestedType || ticket.ScopeId != context.RequestedScopeId)
            return Reject(RecordMismatch);

        if (ticket.Status != TicketStatus.Valid)
            return Reject(ticket.Status == TicketStatus.Blocked ? Blocked : Cancelled);

        if (context.Test)
            return new AdmissionEvaluation(AdmissionVerdict.TestTicket);

        if (context.RequestedType == TicketType.EventTicket)
        {
            if (context.RequestedScopeId != context.EventId)
                return Reject(WrongEvent);
        }
        else
        {
            if (context.EventSeasonId is null)
                return Reject(UnknownEvent);
            if (context.RequestedScopeId != context.EventSeasonId)
                return Reject(WrongSeason);
        }

        if (context.RequestedType == TicketType.SeasonSingle && context.RedeemedEventId is { } bound && bound != context.EventId)
            return Reject(FlexRedeemedElsewhere);

        if (context.RequiresConversion && context.RequestedType != TicketType.EventTicket && !context.IsBoxOfficeFlex)
            return Reject(ConversionRequired);

        if (context.Mode == ScanMode.CheckIn)
        {
            if (context.AdmissionsInside >= context.AdmissionCap)
                return Reject(context.AdmissionCap > 1 ? AllAdmissionsUsed : AlreadyCheckedIn);
            return new AdmissionEvaluation(AdmissionVerdict.Admit);
        }

        if (context.AdmissionsInside <= 0)
            return Reject(NotCheckedIn);

        return new AdmissionEvaluation(AdmissionVerdict.Admit);
    }

    private static AdmissionEvaluation Reject(string reason) => new(AdmissionVerdict.Reject, reason);
}
