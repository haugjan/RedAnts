using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;
using Xunit;

namespace RedAnts.Ticketing.Tests.Tickets;

public class GetEventTicketsForAdminTests
{
    private const int EventId = 10;

    private readonly StubEventTicketListReader _reader = new();

    private GetEventTicketsForAdmin.Handler Handler => new(_reader);

    public GetEventTicketsForAdminTests()
    {
        _reader.Rows.Add(StubEventTicketListReader.Row(EventId, "Muster", redeemed: true, inside: true, bundleId: 1, bundleReference: "Sponsoren"));
        _reader.Rows.Add(StubEventTicketListReader.Row(EventId, "Beispiel", redeemed: true, inside: false, email: "beat@example.ch"));
        _reader.Rows.Add(StubEventTicketListReader.Row(EventId, "Offen"));
        _reader.Rows.Add(StubEventTicketListReader.Row(EventId + 1, "Anderer"));
        _reader.Bundles.Add(new EventTicketBundleRow(1, "Sponsoren", TicketCategory.Adult, 1));
    }

    [Fact]
    public async Task Returns_the_tickets_of_the_event_with_bundles_and_redemption_counts()
    {
        var result = await Handler.HandleAsync(new GetEventTicketsForAdmin.Query(EventId));

        Assert.Equal(3, result.Total);
        Assert.Equal(3, result.Tickets.Count);
        Assert.Equal((1, 1, 1), (result.RedeemedCount, result.OutsideCount, result.OpenCount));
        Assert.Equal("Sponsoren", Assert.Single(result.Bundles).Reference);
    }

    [Fact]
    public async Task Filters_by_bundle_and_keeps_the_totals()
    {
        var result = await Handler.HandleAsync(new GetEventTicketsForAdmin.Query(EventId, BundleId: 1));

        Assert.Equal("Muster", Assert.Single(result.Tickets).Holder!.LastName);
        Assert.Equal(3, result.Total);
    }

    [Theory]
    [InlineData("beispiel", "Beispiel")]
    [InlineData("beat@example", "Beispiel")]
    [InlineData("sponsoren", "Muster")]
    public async Task Searches_holder_email_and_bundle(string search, string expectedLastName)
    {
        var result = await Handler.HandleAsync(new GetEventTicketsForAdmin.Query(EventId, Search: search));

        Assert.Equal(expectedLastName, Assert.Single(result.Tickets).Holder!.LastName);
    }

    [Fact]
    public async Task A_missing_event_yields_nothing()
    {
        var result = await Handler.HandleAsync(new GetEventTicketsForAdmin.Query(0));

        Assert.Same(EventTicketsForAdmin.Empty, result);
    }
}
