namespace RedAnts.Ticketing.Features.Catalog.Shop;

public sealed record UpcomingEvent(
    int Id,
    string Name,
    DateOnly Date,
    TimeOnly StartTime,
    bool TimeUnknown,
    string? VenueName,
    string? ImageUrl,
    string? HomeTeamLogoUrl,
    string? AwayTeamLogoUrl,
    string? Url,
    bool Buyable,
    bool SoldOut);
