namespace RedAnts.Domain.Ticketing.Sales;

public enum TicketType
{
    EventTicket,
    SeasonSingle,
    SeasonPass,
    MemberCard,
    FreeEntry
}

public enum FreeEntryType
{
    Player,
    Staff,
    Official,
    SwissUnihockeyFreeCard,
    Child
}

public enum TicketCategory
{
    Adult,
    AdultPromo,
    Youth,
    YouthPromo,
    Child
}

public enum OrderStatus
{
    Draft,
    Paid,
    Cancelled,
    Refunded
}

public enum TicketStatus
{
    Valid,
    Cancelled,
    Blocked
}

public enum VisitLogType
{
    CheckIn,
    CheckOut
}

public enum PaymentMethod
{
    Payrexx,
    Cash,
    Twint,
    Invoice
}

public enum BuyerType
{
    Private,
    Company
}

public enum AddOnScope
{
    PerPass,
    PerOrder
}

public static class AddOnScopeExtensions
{
    public static string DisplayName(this AddOnScope scope) => scope switch
    {
        AddOnScope.PerPass => "pro Saisonkarte",
        AddOnScope.PerOrder => "einmalig pro Bestellung",
        _ => scope.ToString()
    };
}

public static class BuyerTypeExtensions
{
    public static string DisplayName(this BuyerType type) => type switch
    {
        BuyerType.Private => "Privatperson",
        BuyerType.Company => "Firma",
        _ => type.ToString()
    };
}

public static class FreeEntryTypeExtensions
{
    public static string DisplayName(this FreeEntryType type) => type switch
    {
        FreeEntryType.Player => "Spieler:in",
        FreeEntryType.Staff => "Staff",
        FreeEntryType.Official => "Funktionär",
        FreeEntryType.SwissUnihockeyFreeCard => "SU-Freieintritt",
        FreeEntryType.Child => "Kind (gratis)",
        _ => type.ToString()
    };
}

public static class TicketStatusExtensions
{
    public static string DisplayName(this TicketStatus status) => status switch
    {
        TicketStatus.Valid => "Gültig",
        TicketStatus.Cancelled => "Storniert",
        TicketStatus.Blocked => "Gesperrt",
        _ => status.ToString()
    };
}

public static class TicketCategoryExtensions
{
    public static string DisplayName(this TicketCategory category) => category switch
    {
        TicketCategory.Adult => "Erwachsen",
        TicketCategory.AdultPromo => "Sonderaktion Erwachsen",
        TicketCategory.Youth => "Jugend (bis 19)",
        TicketCategory.YouthPromo => "Sonderaktion Jugend",
        TicketCategory.Child => "Kind (bis 5)",
        _ => category.ToString()
    };

    public static bool IsPromo(this TicketCategory category) =>
        category is TicketCategory.AdultPromo or TicketCategory.YouthPromo;

    public static TicketCategory? PromoCounterpart(this TicketCategory category) => category switch
    {
        TicketCategory.Adult => TicketCategory.AdultPromo,
        TicketCategory.Youth => TicketCategory.YouthPromo,
        _ => null
    };
}
