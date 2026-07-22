using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Tickets;

public static class TicketDisplay
{
    public static string TypeLabel(TicketType type) => type switch
    {
        TicketType.EventTicket => "Spielticket",
        TicketType.SeasonSingle => "Flexticket",
        TicketType.SeasonPass => "Saisonkarte",
        TicketType.MemberCard => "Mitgliederkarte",
        TicketType.FreeEntry => "Freier Eintritt",
        _ => "Ticket"
    };

    public static string Kicker(TicketType type) => type switch
    {
        TicketType.EventTicket => "Einzelspiel",
        TicketType.SeasonSingle => "1 Spiel frei",
        TicketType.SeasonPass => "Ganze Saison",
        TicketType.MemberCard => "Mitglied",
        TicketType.FreeEntry => "Gast",
        _ => "Ticket"
    };

    public static string AccentHex(TicketType type) => type switch
    {
        TicketType.EventTicket => "#C8102E",
        TicketType.SeasonSingle => "#E4720F",
        TicketType.SeasonPass => "#1F5FBF",
        TicketType.MemberCard => "#1A7F37",
        TicketType.FreeEntry => "#6B4EA0",
        _ => "#C8102E"
    };
}
