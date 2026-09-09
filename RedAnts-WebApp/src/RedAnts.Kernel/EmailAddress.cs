using System.Text.RegularExpressions;

namespace RedAnts.Domain;

public readonly partial record struct EmailAddress
{
    public const int MaxLength = 200;

    private readonly string? _value;

    private EmailAddress(string value) => _value = value;

    public string Value => _value ?? "";

    public static EmailAddress Create(string? raw, string field = "email", string label = "E-Mail")
    {
        ValidationException.ThrowIfBlank(raw, field, label);
        var value = raw.Trim();
        ValidationException.ThrowIfLongerThan(value, MaxLength, field, label);
        if (!Pattern().IsMatch(value))
            throw new ValidationException(field, $"{label} ist keine gültige Adresse.");
        return new EmailAddress(value);
    }

    public static EmailAddress? Optional(string? raw, string field = "email", string label = "E-Mail") =>
        string.IsNullOrWhiteSpace(raw) ? null : Create(raw, field, label);

    public static EmailAddress? TryCreate(string? raw)
    {
        try { return Create(raw); }
        catch (ValidationException) { return null; }
    }

    public static bool IsValid(string? raw) => TryCreate(raw) is not null;

    public bool HasDomain(string domain) =>
        Value.EndsWith("@" + domain.TrimStart('@'), StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Value;

    [GeneratedRegex(
        @"^[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?\.)+[A-Za-z]{2,}$",
        RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 250)]
    private static partial Regex Pattern();
}
