namespace RedAnts.Features.Ticketing.Admin;

public sealed record AdminIdentity(string Name, string? Email, bool IsAdmin = false)
{
    public string Initials => AdminFormat.Initials(Name);
}
