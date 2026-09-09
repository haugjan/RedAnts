using RedAnts.Domain;

namespace RedAnts.Ticketing.Features.MemberCards.Admin;

internal static class MemberContactRules
{
    internal static string? FirstError(string? email, DateOnly? birthday)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var e = email.Trim();
            var at = e.IndexOf('@');
            if (at <= 0 || at == e.Length - 1 || !e[(at + 1)..].Contains('.') || e.EndsWith('.'))
                return "Bitte eine gültige E-Mail-Adresse angeben (oder das Feld leer lassen).";
        }
        if (birthday is { } b)
        {
            if (b > SwissTime.Today) return "Das Geburtsdatum darf nicht in der Zukunft liegen.";
            if (b.Year < 1900) return "Bitte ein gültiges Geburtsdatum angeben.";
        }
        return null;
    }
}
