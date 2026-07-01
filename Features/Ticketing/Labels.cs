using RedAnts.Domain.Ticketing;

namespace RedAnts.Features.Ticketing;

/// <summary>German display labels for ticketing enums (public-facing text).</summary>
public static class Labels
{
    public static string PriceCategory(PriceCategory c) => c switch
    {
        Domain.Ticketing.PriceCategory.Child => "Kind",
        Domain.Ticketing.PriceCategory.Youth => "Jugend",
        Domain.Ticketing.PriceCategory.Adult => "Erwachsen",
        _ => c.ToString()
    };

    public static string SeasonTicketCategory(SeasonTicketCategory c) => c switch
    {
        Domain.Ticketing.SeasonTicketCategory.Block4 => "4er-Block",
        Domain.Ticketing.SeasonTicketCategory.Members => "Mitglieder",
        Domain.Ticketing.SeasonTicketCategory.Extern => "Extern",
        _ => c.ToString()
    };

    public static string AgeGroup(AgeGroup a) => a switch
    {
        Domain.Ticketing.AgeGroup.Child => "Kind",
        Domain.Ticketing.AgeGroup.Youth => "Jugend",
        Domain.Ticketing.AgeGroup.Adult => "Erwachsen",
        _ => a.ToString()
    };

    public static string Status(SeasonStatus s) => s switch
    {
        SeasonStatus.Draft => "Entwurf",
        SeasonStatus.Open => "Offen",
        SeasonStatus.Intern => "Intern",
        SeasonStatus.Closed => "Geschlossen",
        _ => s.ToString()
    };

    public static string Status(EventStatus s) => s switch
    {
        EventStatus.Draft => "Entwurf",
        EventStatus.Open => "Offen",
        EventStatus.Intern => "Intern",
        EventStatus.Closed => "Geschlossen",
        _ => s.ToString()
    };

    public static string PayStatus(PayStatus s) => s switch
    {
        Domain.Ticketing.PayStatus.Pending => "Ausstehend",
        Domain.Ticketing.PayStatus.Paid => "Bezahlt",
        Domain.Ticketing.PayStatus.Failed => "Fehlgeschlagen",
        Domain.Ticketing.PayStatus.Refunded => "Rückerstattet",
        _ => s.ToString()
    };
}
