using System.Net.Mail;

namespace RedAnts.Domain;

public readonly record struct EmailAddress
{
    public const int MaxLength = 200;

    public string Value { get; }

    private EmailAddress(string value) => Value = value;

    public static EmailAddress Create(string? raw, string field = "email", string label = "E-Mail")
    {
        ValidationException.ThrowIfBlank(raw, field, label);
        var value = raw.Trim();
        ValidationException.ThrowIfLongerThan(value, MaxLength, field, label);
        if (!MailAddress.TryCreate(value, out var parsed) || parsed.Address != value || !value.Contains('.'))
            throw new ValidationException(field, $"{label} ist keine gültige Adresse.");
        return new EmailAddress(value);
    }

    public static EmailAddress? TryCreate(string? raw)
    {
        try { return Create(raw); }
        catch (ValidationException) { return null; }
    }

    public bool HasDomain(string domain) =>
        Value.EndsWith("@" + domain.TrimStart('@'), StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Value;
}
