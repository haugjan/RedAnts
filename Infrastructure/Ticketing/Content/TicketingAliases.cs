namespace RedAnts.Infrastructure.Ticketing.Content;

/// <summary>Single source of truth for the ticketing Document Type and property aliases.
/// Shared by the content-type seeder and the read-adapters.</summary>
internal static class TicketingAliases
{
    // Document types
    public const string RootType = "ticketingRoot";
    public const string SeasonsFolderType = "seasonsFolder";
    public const string VenuesFolderType = "venuesFolder";
    public const string SeasonType = "season";
    public const string VenueType = "venue";
    public const string EventType = "event";

    // Venue properties
    public const string VenueGoogleGeoId = "googleGeoId";
    public const string VenueImage = "image";
    public const string VenueDescription = "description";

    // Shared access properties (on both season and event)
    public const string AccessSecret = "accessSecret";
    public const string PublicLink = "publicLink";
    public const string InternLink = "internLink";

    // Season properties
    public const string SeasonStartDate = "startDate";
    public const string SeasonEndDate = "endDate";
    public const string SeasonStatus = "status";
    public const string SeasonImage = "image";

    // Event properties
    public const string EventText = "text";
    public const string EventStart = "start";
    public const string EventVenue = "venue";
    public const string EventStatus = "status";
    public const string EventInternalLink = "internalLink";
    public const string EventImage = "eventImage";
    public const string EventHomeTeamLogo = "homeTeamLogo";
    public const string EventAwayTeamLogo = "awayTeamLogo";
    public const string EventPriceChild = "priceChild";
    public const string EventPriceYouth = "priceYouth";
    public const string EventPriceAdult = "priceAdult";
}
