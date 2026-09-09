using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.EventBundles;
using Xunit;

namespace RedAnts.Ticketing.Tests.EventBundles;

public class GetEventBundlesTests
{
    private const int EventId = 11;

    private static EventBundleTicketRow Ticket(string reference) =>
        new(Guid.NewGuid(), EventId, reference, "https://t/x", TicketCategory.Adult,
            CardHolder.Create(BuyerType.Private, null, null, "Anna", "Muster", null, null, null, null, null, null, null, null));

    [Fact]
    public async Task Bundles_come_from_the_list_reader_for_the_event()
    {
        var reader = new FakeEventBundleListReader();
        reader.Rows.Add(new EventBundleRow(1, EventId, TicketCategory.Adult, "Sponsor", SwissTime.Timestamp, 3, 1));
        reader.Rows.Add(new EventBundleRow(2, 99, TicketCategory.Adult, "Anderer Anlass", SwissTime.Timestamp, 1, 0));

        var bundles = await new GetEventBundles.Handler(reader).HandleAsync(new GetEventBundles.Query(EventId));

        Assert.Equal(EventId, Assert.Single(reader.Requested));
        Assert.Equal("Sponsor", Assert.Single(bundles).Reference);
    }

    [Fact]
    public async Task Tickets_of_one_bundle_and_of_several_bundles_for_the_export()
    {
        var reader = new FakeEventBundleTicketsReader();
        reader.ByBundle[1] = [Ticket("A"), Ticket("A")];
        reader.ByBundle[2] = [Ticket("B")];

        var single = await new GetEventBundleTickets.Handler(reader).HandleAsync(new GetEventBundleTickets.Query(2));
        var export = await new GetEventBundlesForExport.Handler(reader).HandleAsync(new GetEventBundlesForExport.Query([1, 2]));

        Assert.Equal("B", Assert.Single(single).Reference);
        Assert.Equal(3, export.Count);
        Assert.Equal(["bundle:2", "bundles:1,2"], reader.Calls);
        Assert.Equal("Erwachsen", export[0].CategoryLabel);
    }
}
