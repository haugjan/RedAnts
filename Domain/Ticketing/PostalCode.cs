namespace RedAnts.Domain.Ticketing;

/// <summary>Postal code value object. Swiss codes must be 4 digits (1000–9999);
/// foreign codes are accepted leniently. Mirrors the reference project's PostalCode.</summary>
public sealed record PostalCode
{
    public string Value { get; }

    private PostalCode(string value) => Value = value;

    public static PostalCode Create(string value, string? country = null)
    {
        var trimmed = value?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new DomainException("PLZ ist erforderlich.");

        if (IsSwiss(country))
        {
            if (trimmed.Length != 4 || !trimmed.All(char.IsDigit)
                || int.Parse(trimmed) is < 1000 or > 9999)
                throw new DomainException("Schweizer PLZ muss 4-stellig sein (1000–9999).");
        }
        else if (trimmed.Length > 10)
        {
            throw new DomainException("Ungültige PLZ.");
        }

        return new PostalCode(trimmed);
    }

    /// <summary>Rehydrates from storage without validation so legacy/imported values never block loading.</summary>
    public static PostalCode FromPersistence(string? value) => new((value ?? "").Trim());

    private static bool IsSwiss(string? country)
    {
        var c = country?.Trim();
        return string.IsNullOrEmpty(c)
            || c.Equals("Schweiz", StringComparison.OrdinalIgnoreCase)
            || c.Equals("Switzerland", StringComparison.OrdinalIgnoreCase)
            || c.Equals("CH", StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString() => Value;
}
