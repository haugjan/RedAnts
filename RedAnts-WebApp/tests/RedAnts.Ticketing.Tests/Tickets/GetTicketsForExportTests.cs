using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.EventBundles.Admin;
using RedAnts.Ticketing.Features.Tickets;
using RedAnts.Ticketing.Tests.Checkout;
using Xunit;

namespace RedAnts.Ticketing.Tests.Tickets;

public class GetTicketsForExportTests
{
    [Fact]
    public async Task Maps_the_bundle_tickets_to_export_rows_with_links()
    {
        var bundles = new StubEventBundleTickets();
        var uuid = Guid.Parse("0123456789abcdef0123456789abcdef");
        var holder = CardHolder.Create(BuyerType.Private, null, null, "Anna", "Muster", null, null, null, null, null, null, null, null);
        bundles.Bundles[4] = [new EventBundleTicket(uuid, 10, "Sponsoren", TicketCategory.Youth, holder)];
        bundles.Bundles[5] = [new EventBundleTicket(Guid.NewGuid(), 10, "Presse")];
        var handler = new GetTicketsForExport.Handler(bundles, new StubTicketTokens(), new StubPublicBaseUrl());

        var rows = await handler.HandleAsync(new GetTicketsForExport.Query([4, 5]));

        Assert.Equal([4, 5], bundles.LastBundleIds);
        Assert.Equal(2, rows.Count);
        Assert.Equal("01234567", rows[0].CardNo);
        Assert.Equal("Sponsoren", rows[0].Bundle);
        Assert.Equal(TicketCategory.Youth.DisplayName(), rows[0].Category);
        Assert.Equal("Muster", rows[0].Holder.LastName);
        Assert.Equal("https://tickets.test/ticket/s-01234567", rows[0].Link);
        Assert.Equal(CardHolder.Empty, rows[1].Holder);
    }
}
