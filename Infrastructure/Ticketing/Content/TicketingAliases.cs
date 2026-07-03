namespace RedAnts.Infrastructure.Ticketing.Content;

/// <summary>Single source of truth for the ticketing Document Type and property aliases.
/// Shared by the content-type seeder and the read-adapters.</summary>
internal static class TicketingAliases
{
    // Document types
    public const string RootType = "ticketingRoot";
    public const string SeasonsFolderType = "seasonsFolder";
    public const string VenuesFolderType = "venuesFolder";
    public const string CategoriesFolderType = "ticketCategoriesFolder";
    public const string SeasonType = "season";
    public const string VenueType = "venue";
    public const string EventType = "event";
    public const string TicketCategoryType = "ticketCategory";

    // Ticket category (central content list; referenced by the sales-price blocks)
    public const string CategoryCode = "code";
    public const string CategoryDefaultPrice = "defaultPrice";

    // Sales-price block (element type used in the "salesPrices" Block List on event and season)
    public const string SalesPriceElement = "salesPrice";
    public const string SalesPriceCategory = "category";
    public const string SalesPriceUseDefault = "useDefaultPrice";
    public const string SalesPricePrice = "price";
    public const string SalesPriceContingent = "contingent";
    // Block List property carrying the sales prices (on both event and season)
    public const string SalesPrices = "salesPrices";

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
    public const string EventImage = "eventImage";
    public const string EventHomeTeamLogo = "homeTeamLogo";
    public const string EventAwayTeamLogo = "awayTeamLogo";
}
