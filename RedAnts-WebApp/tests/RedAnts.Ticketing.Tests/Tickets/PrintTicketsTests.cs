using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;
using Xunit;

namespace RedAnts.Ticketing.Tests.Tickets;

public class PrintTicketsTests
{
    private readonly StubPrintItems _items = new();
    private readonly RecordingTicketPrinter _printer = new();
    private readonly StubTicketPrintSettings _settings = new();

    private PrintTickets.Handler Handler => new(_items, _printer, _settings);

    private static readonly TicketPrintLayout Layout = TicketPrintLayout.Default with { QrSizeMm = 30 };

    [Fact]
    public async Task Builds_the_pdf_for_the_resolved_items_and_remembers_the_layout()
    {
        _items.Items.Add(new TicketPrintItem(Guid.NewGuid(), "Anna Muster"));

        var pdf = await Handler.HandleAsync(new PrintTickets.Command(TicketType.EventTicket, [9], Layout, 4, null, null, null));

        Assert.Equal([1, 2, 3], pdf);
        Assert.Equal((TicketType.EventTicket, 4, null, null, null), _items.LastCall);
        var build = Assert.Single(_printer.Builds);
        Assert.Same(Layout, build.Layout);
        Assert.Equal((TicketType.EventTicket, Layout), Assert.Single(_settings.Saved));
    }

    [Fact]
    public async Task Nothing_to_print_returns_null_and_saves_nothing()
    {
        var pdf = await Handler.HandleAsync(new PrintTickets.Command(TicketType.SeasonPass, [9], Layout, null, 3, "Ref", null));

        Assert.Null(pdf);
        Assert.Empty(_printer.Builds);
        Assert.Empty(_settings.Saved);
    }
}
