using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public static class TicketTypeExtensions
{
    public static string DisplayName(this TicketType type) => type switch
    {
        TicketType.EventTicket => "Spieltickets",
        TicketType.SeasonSingle => "Flextickets",
        TicketType.SeasonPass => "Saisonkarten",
        TicketType.MemberCard => "Mitglieder",
        TicketType.FreeEntry => "Berechtigte",
        _ => type.ToString()
    };
}
