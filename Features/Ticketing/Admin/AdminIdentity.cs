namespace RedAnts.Features.Ticketing.Admin;

public sealed record AdminIdentity(string Name, string? Email)
{
    public string Initials => AdminFormat.Initials(Name);
}
