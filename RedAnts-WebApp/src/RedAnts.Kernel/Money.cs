namespace RedAnts.Domain;

public readonly record struct Money(decimal Amount, string Currency)
{
    public const string Chf = "CHF";

    public static Money Zero => new(0m, Chf);

    public static Money Of(decimal amount, string currency = Chf, string field = "amount", string label = "Betrag")
    {
        ValidationException.ThrowIfNegative(amount, field, label);
        return new Money(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), currency);
    }

    public bool IsZero => Amount == 0m;

    public Money Add(Money other) => new(Amount + SameCurrency(other).Amount, Currency);

    public Money Subtract(Money other) => new(Amount - SameCurrency(other).Amount, Currency);

    public Money Times(int factor) => new(Amount * factor, Currency);

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    private Money SameCurrency(Money other) =>
        other.Currency == Currency
            ? other
            : throw new DomainException($"Währungen stimmen nicht überein ({Currency} vs. {other.Currency}).");

    public override string ToString() => $"{Currency} {Amount:0.00}";
}
