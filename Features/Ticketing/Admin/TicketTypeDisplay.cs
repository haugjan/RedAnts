using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

/// <summary>Canonical German labels for the ticket types, used to keep the admin naming consistent.
/// These are the section/group labels (plural), mirroring the <see cref="TicketCategoryExtensions"/>
/// pattern. Kept in the admin feature (not in the domain enum file) to avoid touching the shared
/// SalesEnums during parallel work; may be relocated next to the enum later.</summary>
public static class TicketTypeExtensions
{
    public static string DisplayName(this TicketType type) => type switch
    {
        TicketType.EventTicket => "Einzeleintritte",
        TicketType.SeasonSingle => "Einzelspiele",
        TicketType.SeasonPass => "Saisonkarten",
        TicketType.MemberCard => "Mitglieder",
        TicketType.FreeEntry => "Berechtigte",
        _ => type.ToString()
    };
}
