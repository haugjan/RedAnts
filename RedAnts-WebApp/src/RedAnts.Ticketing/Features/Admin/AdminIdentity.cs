namespace RedAnts.Ticketing.Features.Admin;

public sealed record AdminIdentity(string Name, string? Email, bool IsAdmin = false)
{
    public string Initials => AdminFormat.Initials(Name);
}
