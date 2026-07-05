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
        TicketCategory.Youth => "Jugend",
        TicketCategory.YouthReduced => "Jugend reduziert",
        TicketCategory.Child => "Kind",
        _ => category.ToString()
    };
}
