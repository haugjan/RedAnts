namespace RedAnts.Ticketing.Features.Helpers;

public sealed record HelperRow(
    int Id,
    int SeasonId,
    string FirstName,
    string LastName,
    string Email,
    string Code,
    bool AllEvents,
    IReadOnlyList<int> EventIds,
    bool CanRebook,
    bool Active,
    DateTimeOffset CreatedAt)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}
