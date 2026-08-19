using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Scanning;

namespace RedAnts.Infrastructure.Ticketing.Scanning;

public enum AdmissionVerdict { TestEmpty, TestTicket, Admit, Reject }

public sealed record ScannedTicketFacts(TicketType Type, int ScopeId, TicketStatus Status);

public sealed record AdmissionEvaluation(AdmissionVerdict Verdict, string? Reason = null);

public static class AdmissionRules
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

    public static AdmissionEvaluation Evaluate(
        int eventId,
        TicketType requestedType,
        int requestedScopeId,
        ScanMode mode,
        bool test,
        bool isEmptyUuid,
        ScannedTicketFacts? ticket,
        int? eventSeasonId,
        int? redeemedEventId,
        int admissionsInside,
        int admissionCap,
        bool requiresConversion,
        bool isBoxOfficeFlex)
    {
        if (isEmptyUuid)
            return new AdmissionEvaluation(AdmissionVerdict.TestEmpty);

        if (ticket is null)
            return Reject(UnknownTicket);

        if (ticket.Type != requestedType || ticket.ScopeId != requestedScopeId)
            return Reject(RecordMismatch);

        if (ticket.Status != TicketStatus.Valid)
            return Reject(ticket.Status == TicketStatus.Blocked ? Blocked : Cancelled);

        if (test)
            return new AdmissionEvaluation(AdmissionVerdict.TestTicket);

        if (requestedType == TicketType.EventTicket)
        {
            if (requestedScopeId != eventId)
                return Reject(WrongEvent);
        }
        else
        {
            if (eventSeasonId is null)
                return Reject(UnknownEvent);
            if (requestedScopeId != eventSeasonId)
                return Reject(WrongSeason);
        }

        if (requestedType == TicketType.SeasonSingle && redeemedEventId is { } bound && bound != eventId)
            return Reject(FlexRedeemedElsewhere);

        if (requiresConversion && requestedType != TicketType.EventTicket && !isBoxOfficeFlex)
            return Reject(ConversionRequired);

        if (mode == ScanMode.CheckIn)
        {
            if (admissionsInside >= admissionCap)
                return Reject(admissionCap > 1 ? AllAdmissionsUsed : AlreadyCheckedIn);
            return new AdmissionEvaluation(AdmissionVerdict.Admit);
        }

        if (admissionsInside <= 0)
            return Reject(NotCheckedIn);

        return new AdmissionEvaluation(AdmissionVerdict.Admit);
    }

    private static AdmissionEvaluation Reject(string reason) =>
        new(AdmissionVerdict.Reject, reason);
}
