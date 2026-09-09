namespace RedAnts.Domain;

public readonly record struct Money : IComparable<Money>
{
    public const string SwissFranc = "CHF";

    private const decimal FiveRappen = 0.05m;

    private readonly string? _currency;

    private Money(decimal amount, string? currency)
    {
        Amount = amount;
        _currency = currency;
    }

    public decimal Amount { get; }

    public string Currency => _currency ?? SwissFranc;

    public static Money Zero { get; } = new(0m, SwissFranc);

    public static Money Of(decimal amount, string currency = SwissFranc, string field = "amount", string label = "Betrag")
    {
        ValidationException.ThrowIfNegative(amount, field, label);
        return new Money(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), Normalize(currency));
    }

    public static Money Chf(decimal amount, string field = "price", string label = "Preis")
    {
        ValidationException.ThrowIfNegative(amount, field, label);
        return new Money(RoundToFiveRappen(amount), SwissFranc);
    }

    public static Money Stored(decimal amount, string? currency = SwissFranc) => new(amount, Normalize(currency));

    public static decimal RoundToFiveRappen(decimal amount) =>
        decimal.Round(decimal.Round(amount / FiveRappen, 0, MidpointRounding.AwayFromZero) * FiveRappen, 2);

    public bool IsZero => Amount == 0m;

    public bool IsPositive => Amount > 0m;

    public Money Add(Money other) => new(Amount + AmountOf(other), Currency);

    public Money Subtract(Money other) => new(Amount - AmountOf(other), Currency);

    public Money Times(int factor) => new(Amount * factor, Currency);

    public static Money Sum(IEnumerable<Money> amounts)
    {
        var total = Zero;
        var empty = true;
        foreach (var amount in amounts)
        {
            total = empty ? amount : total.Add(amount);
            empty = false;
        }
        return total;
    }

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;

    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

    public int CompareTo(Money other) => Amount.CompareTo(AmountOf(other));

    public bool Equals(Money other) => Amount == other.Amount && Currency == other.Currency;

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Currency} {Amount:0.00}";

    private decimal AmountOf(Money other) =>
        other.Currency == Currency
            ? other.Amount
            : throw new DomainException($"Währungen stimmen nicht überein ({Currency} vs. {other.Currency}).");

    private static string Normalize(string? currency) =>
        string.IsNullOrWhiteSpace(currency) ? SwissFranc : currency.Trim().ToUpperInvariant();
}
