using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.EventBundles;
using Xunit;

namespace RedAnts.Ticketing.Tests.EventBundles;

public class EventBundleTests
{
    private const int EventId = 10;

    [Fact]
    public async Task Bundle_creation_validates_reference_quantity_and_uniqueness()
    {
        var bundles = new RecordingEventTicketBundles();
        bundles.Existing.Add("Sponsor A");
        var handler = new CreateEventTicketBundle.Handler(bundles);

        var blank = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new CreateEventTicketBundle.Command(EventId, TicketCategory.Adult, "  ", 2, null, null, null)));
        var quantity = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new CreateEventTicketBundle.Command(EventId, TicketCategory.Adult, "Neu", 0, null, null, null)));
        var duplicate = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new CreateEventTicketBundle.Command(EventId, TicketCategory.Adult, "sponsor a", 2, null, null, null)));

        Assert.Equal("Bitte ein Bundle angeben.", blank.Message);
        Assert.Equal("Menge muss mindestens 1 sein.", quantity.Message);
        Assert.Contains("bereits vergeben", duplicate.Message);
        Assert.Empty(bundles.Created);
    }

    [Fact]
    public async Task Bundle_creation_trims_the_reference_and_passes_the_order()
    {
        var bundles = new RecordingEventTicketBundles();

        var bundleId = await new CreateEventTicketBundle.Handler(bundles).HandleAsync(
            new CreateEventTicketBundle.Command(EventId, TicketCategory.Youth, " Sponsor B ", 3, "admin", "admin@redants.ch", 42));

        var created = Assert.Single(bundles.Created);
        Assert.Equal(("Sponsor B", 3, 42), (created.Reference, created.Quantity, created.OrderId));
        Assert.Equal(1, bundleId);
    }
}
