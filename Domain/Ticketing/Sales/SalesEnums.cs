namespace RedAnts.Domain.Ticketing.Sales;

/// <summary>The four sellable/scannable ticket types.</summary>
public enum TicketType
{
    EventTicket,
    SeasonSingle,
    SeasonPass,
    MemberCard
}

/// <summary>Lifecycle of a Bestellung (order) = the immutable financial record.
/// Never hard-deleted; cancellations/refunds are status changes, not row deletions (OR 958f).</summary>
public enum OrderStatus
{
    Draft,
    Paid,
    Cancelled,
    Refunded
}

/// <summary>Validity of an individual ticket.</summary>
public enum TicketStatus
{
    Valid,
    Cancelled
}
