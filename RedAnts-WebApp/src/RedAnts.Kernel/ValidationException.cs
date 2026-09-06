using System.Diagnostics.CodeAnalysis;

namespace RedAnts.Domain;

public sealed class ValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;

    public static void ThrowIfBlank([NotNull] string? value, string field, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException(field, $"{label} ist Pflicht.");
    }

    public static void ThrowIfLongerThan(string value, int maxLength, string field, string label)
    {
        if (value.Length > maxLength)
            throw new ValidationException(field, $"{label} darf höchstens {maxLength} Zeichen lang sein.");
    }

    public static void ThrowIfNegative(decimal value, string field, string label)
    {
        if (value < 0)
            throw new ValidationException(field, $"{label} darf nicht negativ sein.");
    }
}
