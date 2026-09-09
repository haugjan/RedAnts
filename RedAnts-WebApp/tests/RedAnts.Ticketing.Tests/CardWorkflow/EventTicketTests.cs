using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.CardWorkflow;
using RedAnts.Ticketing.Features.Ports;
using Xunit;

namespace RedAnts.Ticketing.Tests.CardWorkflow;

public class EventTicketTests
{
    private const int EventId = 10;

    private static EventTicket Stored(InMemoryEventTickets tickets, TicketStatus status = TicketStatus.Valid, bool redeemed = false)
    {
        var ticket = EventTicket.FromPersistence(5, Guid.NewGuid(), EventId, TicketCategory.Adult, 20m, 7, status,
            new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero), redeemed, Buyer.Create(BuyerType.Private, "Anna", "Muster", null),
            "admin", "admin@redants.ch", 3, tierId: 2);
        tickets.Stored.Add(ticket);
        return ticket;
    }

    [Fact]
    public async Task Edit_replaces_the_editable_fields_and_keeps_the_rest()
    {
        var tickets = new InMemoryEventTickets();
        var original = Stored(tickets);
        var handler = new EditEventTicket.Handler(tickets);

        await handler.HandleAsync(new EditEventTicket.Command(original.Uuid, EventId, TicketCategory.Youth, 12.346m, TicketStatus.Valid, true, null));

        var saved = Assert.Single(tickets.Stored);
        Assert.Equal(TicketCategory.Youth, saved.Category);
        Assert.Equal(12.35m, saved.Price);
        Assert.True(saved.Redeemed);
        Assert.Equal(original.Id, saved.Id);
        Assert.Equal(original.OrderId, saved.OrderId);
        Assert.Equal(original.CreatedAt, saved.CreatedAt);
        Assert.Equal(original.CreatedByName, saved.CreatedByName);
        Assert.Equal(original.BundleId, saved.BundleId);
        Assert.Equal(original.TierId, saved.TierId);
        Assert.Equal("Anna", saved.Buyer?.FirstName);
    }

    [Fact]
    public async Task Edit_forces_not_redeemed_for_cancelled_tickets()
    {
        var tickets = new InMemoryEventTickets();
        var original = Stored(tickets, redeemed: true);

        await new EditEventTicket.Handler(tickets).HandleAsync(
            new EditEventTicket.Command(original.Uuid, EventId, TicketCategory.Adult, 20m, TicketStatus.Cancelled, true, null));

        var saved = Assert.Single(tickets.Stored);
        Assert.Equal(TicketStatus.Cancelled, saved.Status);
        Assert.False(saved.Redeemed);
    }

    [Fact]
    public async Task Edit_rejects_a_negative_price_without_saving()
    {
        var tickets = new InMemoryEventTickets();
        var original = Stored(tickets);

        await Assert.ThrowsAsync<DomainException>(() => new EditEventTicket.Handler(tickets).HandleAsync(
            new EditEventTicket.Command(original.Uuid, EventId, TicketCategory.Adult, -1m, TicketStatus.Valid, false, null)));

        Assert.Equal(20m, Assert.Single(tickets.Stored).Price);
    }

    [Fact]
    public async Task Edit_rejects_an_unknown_ticket()
    {
        var tickets = new InMemoryEventTickets();
        Stored(tickets);

        var ex = await Assert.ThrowsAsync<DomainException>(() => new EditEventTicket.Handler(tickets).HandleAsync(
            new EditEventTicket.Command(Guid.NewGuid(), EventId, TicketCategory.Adult, 20m, TicketStatus.Valid, false, null)));

        Assert.Equal("Ticket wurde nicht gefunden.", ex.Message);
    }

    [Fact]
    public async Task Holder_and_deletion_delegate_to_the_ports()
    {
        var tickets = new InMemoryEventTickets();
        var deletion = new RecordingTicketDeletion();
        var uuid = Guid.NewGuid();
        var holder = CardHolder.Create(BuyerType.Private, null, null, "Anna", "Muster", null, null, null, null, null, null, null, null);

        await new SetEventTicketHolder.Handler(tickets).HandleAsync(new SetEventTicketHolder.Command(uuid, holder));
        await new DeleteEventTicket.Handler(deletion).HandleAsync(new DeleteEventTicket.Command(uuid));

        Assert.Equal(uuid, Assert.Single(tickets.Holders).Uuid);
        Assert.Equal($"event:{uuid}", Assert.Single(deletion.Deleted));
    }

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

        var view = await new CreateEventTicketBundle.Handler(bundles).HandleAsync(
            new CreateEventTicketBundle.Command(EventId, TicketCategory.Youth, " Sponsor B ", 3, "admin", "admin@redants.ch", 42));

        var created = Assert.Single(bundles.Created);
        Assert.Equal(("Sponsor B", 3, 42), (created.Reference, created.Quantity, created.OrderId));
        Assert.Equal("Sponsor B", view.Reference);
    }

    [Fact]
    public async Task Import_delegates_to_the_bundle_port()
    {
        var bundles = new RecordingEventTicketBundles();
        var rows = new List<TicketImportRow>
        {
            new(null, null, null, null, CardHolder.Create(BuyerType.Private, null, null, "A", "B", null, null, null, null, null, null, null, null))
        };

        var result = await new ImportEventTickets.Handler(bundles).HandleAsync(
            new ImportEventTickets.Command(EventId, rows, "Import", TicketCategory.Adult, null, null));

        Assert.Equal((1, 0), result);
        Assert.Equal((EventId, "Import"), Assert.Single(bundles.Imports));
    }
}
