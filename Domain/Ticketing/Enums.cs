namespace RedAnts.Domain.Ticketing;

/// <summary>Lifecycle status shared by Season and Event.
/// Draft = not visible; Open = public + bookable; Intern = hidden, reachable only via direct (Sqid) link; Closed = archived.</summary>
public enum SeasonStatus
{
    Draft,
    Open,
    Intern,
    Closed
}

/// <inheritdoc cref="SeasonStatus"/>
public enum EventStatus
{
    Draft,
    Open,
    Intern,
    Closed
}

/// <summary>Price tier of a single (per-event) ticket.</summary>
public enum PriceCategory
{
    Child,
    Youth,
    Adult
}

/// <summary>Category of a season ticket.</summary>
public enum SeasonTicketCategory
{
    Block4,
    Members,
    Extern
}

/// <summary>Age group captured on a season ticket.</summary>
public enum AgeGroup
{
    Child,
    Youth,
    Adult
}

/// <summary>How a ticket was/will be paid.</summary>
public enum PaymentMethod
{
    Payrexx,
    Cash,
    Twint,
    Invoice
}

/// <summary>Payment lifecycle of a ticket.</summary>
public enum PayStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}

/// <summary>Direction of a scan event written to the scan log.</summary>
public enum ScanDirection
{
    In,
    Out
}

/// <summary>Which kind of ticket a scan/admission refers to.</summary>
public enum TicketKind
{
    Single,
    Season
}
