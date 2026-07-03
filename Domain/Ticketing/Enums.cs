namespace RedAnts.Domain.Ticketing;

/// <summary>Lifecycle status shared by Season and Event.
/// Draft = not visible; Open = public; Intern = hidden, reachable only via direct link; Closed = archived.</summary>
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

/// <summary>How an order was/will be paid.</summary>
public enum PaymentMethod
{
    Payrexx,
    Cash,
    Twint,
    Invoice
}
