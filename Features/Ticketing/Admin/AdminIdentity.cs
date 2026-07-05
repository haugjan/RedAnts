namespace RedAnts.Features.Ticketing.Admin;

public sealed record AdminIdentity(string Name, string? Email)
{
    public string Initials
    {
        get
        {
            var parts = Name.Split(new[] { ' ', '.', '@' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant(),
                _ => $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}"
            };
        }
    }
}
