namespace RedAnts.Infrastructure.Website;

/// <summary>Single source of truth for the public website Document Type and property aliases
/// (FlexPage + Hero element). Separate slice from the ticketing content types.</summary>
internal static class WebsiteAliases
{
    // Document / element types
    public const string FlexPageType = "flexPage";
    public const string HeroElementType = "heroElement";
    public const string HeaderElementType = "headerElement";
    public const string EventListElementType = "eventListElement";

    // FlexPage properties
    public const string FlexContent = "content";

    // Header element properties (sub-page title band, à la Sporthalle "bhe")
    public const string HeaderTitle = "titel";
    public const string HeaderImage = "bild";

    // Event list element properties
    public const string EventListSeason = "season";

    // Hero element properties
    public const string HeroImage = "heroImage";
    public const string HeroTitle = "titel";
    public const string HeroSubtitle = "untertitel";
    public const string HeroTags = "schlagwoerter";
    public const string HeroCtaText = "ctaText";
    public const string HeroCtaLink = "ctaLink";
}
