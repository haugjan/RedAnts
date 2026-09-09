using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Features.Catalog.Shop;
using RedAnts.Ticketing.Features.Checkout;
using RedAnts.Ticketing.Tests.Catalog;
using Xunit;

namespace RedAnts.Ticketing.Tests.Catalog.Shop;

public class ShopQueryTests
{
    private static readonly AvailableTicketCategory Adult = new(1, "Erwachsen", 20m, true, 12);

    private static UpcomingEvent Upcoming(int id, bool soldOut = false) =>
        new(id, "Red Ants vs. Gegner", new DateOnly(2026, 10, 3), new TimeOnly(18, 0), false, "Halle", null, null, null, "/event/", !soldOut, soldOut);

    [Fact]
    public async Task Ticketing_home_passes_events_and_pass_offers_through()
    {
        var reader = new StubTicketingHome
        {
            Home = new TicketingHome([Upcoming(1), Upcoming(2, soldOut: true)],
                [new SeasonPassOffers(3, "Saison 2026/27", "/seasons/2026/", [new PassOffer(Adult, [])])])
        };

        var home = await new GetTicketingHome.Handler(reader).HandleAsync(new GetTicketingHome.Query());

        Assert.Equal(2, home.Events.Count);
        Assert.True(home.Events[1].SoldOut);
        Assert.Equal("Erwachsen", Assert.Single(Assert.Single(home.PassOffers).Offers).Category.Name);
    }

    [Fact]
    public async Task Event_for_sale_is_null_for_an_unknown_event()
    {
        var reader = new StubEventForSale();
        reader.Events[42] = new EventForSale(42, "Red Ants vs. Gegner", null, new DateOnly(2026, 10, 3), new TimeOnly(18, 0), false,
            null, null, null, new VenueForSale(7, "Halle", null, null, null, null), "/event/", [Adult], false, true);

        var found = await new GetEventForSale.Handler(reader).HandleAsync(new GetEventForSale.Query(42));
        var missing = await new GetEventForSale.Handler(reader).HandleAsync(new GetEventForSale.Query(43));

        Assert.NotNull(found);
        Assert.Equal("Halle", found.Venue?.Name);
        Assert.True(found.OffersConversion);
        Assert.Null(missing);
    }

    [Fact]
    public async Task Season_for_sale_returns_the_season_with_its_events()
    {
        var reader = new StubSeasonForSale();
        reader.Seasons[3] = new SeasonForSale(3, "Saison 2026/27", new DateOnly(2026, 8, 1), new DateOnly(2027, 5, 31), null, "/seasons/2026/", [Upcoming(1)]);

        var season = await new GetSeasonForSale.Handler(reader).HandleAsync(new GetSeasonForSale.Query(3));

        Assert.NotNull(season);
        Assert.Single(season.Events);
        Assert.Null(await new GetSeasonForSale.Handler(reader).HandleAsync(new GetSeasonForSale.Query(4)));
    }

    [Fact]
    public async Task Season_pass_offers_list_every_open_season()
    {
        var reader = new StubSeasonForSale();
        reader.PassOffers.Add(new SeasonPassOffers(3, "Saison 2026/27", "/seasons/2026/", [new PassOffer(Adult, [new PassAddOn(9, "Livestream", null, 30m, true, null)])]));
        reader.PassOffers.Add(new SeasonPassOffers(4, "Saison 2027/28", "/seasons/2027/", []));

        var offers = await new GetSeasonPassOffers.Handler(reader).HandleAsync(new GetSeasonPassOffers.Query());

        Assert.Equal(2, offers.Count);
        Assert.True(Assert.Single(Assert.Single(offers[0].Offers).AddOns).OncePerOrder);
        Assert.Empty(offers[1].Offers);
    }

    [Fact]
    public async Task Next_event_is_whatever_the_reader_found()
    {
        var reader = new StubNextEvent();
        Assert.Null(await new GetNextEvent.Handler(reader).HandleAsync(new GetNextEvent.Query()));

        reader.Next = new NextEvent(42, "Red Ants vs. Gegner", new DateOnly(2026, 10, 3), new TimeOnly(18, 0), false, null, null, null,
            "Halle", "/event/", "https://tickets.test/event/", [Adult]);

        var next = await new GetNextEvent.Handler(reader).HandleAsync(new GetNextEvent.Query());

        Assert.Equal(42, next?.Id);
        Assert.Equal("https://tickets.test/event/", next?.AbsoluteUrl);
    }

    [Fact]
    public async Task Venue_is_mapped_to_its_detail()
    {
        var venues = new StubVenues();
        venues.Venues.Add(Venue.FromPersistence(7, "Halle", "geo-1", "/media/halle.jpg", "<p>Beschreibung</p>", "Strasse 1"));

        var detail = await new GetVenue.Handler(venues).HandleAsync(new GetVenue.Query(7));

        Assert.Equal(new VenueDetail(7, "Halle", "geo-1", "/media/halle.jpg", "<p>Beschreibung</p>", "Strasse 1"), detail);
        Assert.Null(await new GetVenue.Handler(venues).HandleAsync(new GetVenue.Query(8)));
    }
}
