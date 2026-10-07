namespace RedAnts.Ticketing.Features.Catalog.Shop;

public sealed record ExternalEvent(
    int Id,
    string Name,
    int SeasonId,
    DateOnly Date,
    TimeOnly StartTime,
    bool TimeUnknown,
    string? Location,
    string? HomeTeamLogoUrl,
    string? AwayTeamLogoUrl);
