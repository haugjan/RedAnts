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
    Cancelled
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
