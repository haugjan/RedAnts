namespace RedAnts.Domain.Ticketing.Sales;

/// <summary>Every kind of admitted entity at an event. The first four are purchased/scannable tickets;
/// <see cref="FreeEntry"/> covers persons who are entitled without a ticket (players, staff, officials,
/// free cards) and are captured spontaneously at the door — their specific kind is a
/// <see cref="FreeEntryType"/> on the free-entry record. Stored as its integer value.</summary>
public enum TicketType
{
    EventTicket,
    SeasonSingle,
    SeasonPass,
    MemberCard,
    FreeEntry
}

/// <summary>The kind of free-entry person (no ticket, admitted by entitlement). Stored as its integer value.</summary>
public enum FreeEntryType
{
    Player,
    Staff,
    Official,
    SwissUnihockeyFreeCard
}

/// <summary>Purchasable ticket category. Replaces the former code/name master data; stored as its
/// integer value. Display names come from <see cref="TicketCategoryExtensions.DisplayName"/>.</summary>
public enum TicketCategory
{
    Adult,
    AdultReduced,
    Youth,
    YouthReduced,
    Child
}

/// <summary>Lifecycle of a Bestellung (order) = the immutable financial record.
/// Never hard-deleted; cancellations/refunds are status changes, not row deletions (OR 958f).
/// Stored as its integer value.</summary>
public enum OrderStatus
{
    Draft,
    Paid,
    Cancelled,
    Refunded
}

/// <summary>Validity of an individual ticket. Stored as its integer value.</summary>
public enum TicketStatus
{
    Valid,
    Cancelled
}

/// <summary>Direction of a single admission scan, recorded in TicketEventVisitsLogs.</summary>
public enum VisitLogType
{
    CheckIn,
    CheckOut
}

/// <summary>How an order was/will be paid. Stored as its integer value.</summary>
public enum PaymentMethod
{
    Payrexx,
    Cash,
    Twint,
    Invoice
}

public static class TicketCategoryExtensions
{
    /// <summary>German display label for a category (was previously stored per row as CategoryName).</summary>
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
