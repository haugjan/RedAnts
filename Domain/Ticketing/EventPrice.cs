namespace RedAnts.Domain.Ticketing;

/// <summary>A price for one price category of an event (in CHF). Owned child of <see cref="Event"/>.</summary>
public sealed record EventPrice
{
    public PriceCategory Category { get; }
    public decimal Amount { get; }

    public EventPrice(PriceCategory category, decimal amount)
    {
        if (amount < 0) throw new DomainException("Preis darf nicht negativ sein.");
        Category = category;
        Amount = decimal.Round(amount, 2);
    }
}
