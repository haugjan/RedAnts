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
    SwissUnihockeyFreeCard
}

public enum TicketCategory
{
    Adult,
    AdultReduced,
    Youth,
    YouthReduced,
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
        TicketCategory.AdultReduced => "Erwachsen reduziert",
        TicketCategory.Youth => "Jugend (bis 16)",
        TicketCategory.YouthReduced => "Jugend reduziert",
        TicketCategory.Child => "Kind (bis 6)",
        _ => category.ToString()
    };
}
