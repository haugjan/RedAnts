using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;
using Xunit;

namespace RedAnts.Ticketing.Tests.Tickets;

public class GetTicketPrintSettingsTests
{
    [Fact]
    public async Task Returns_the_stored_layout_or_the_default()
    {
        var settings = new StubTicketPrintSettings();
        var custom = TicketPrintLayout.Default with { PageWidthMm = 86, ShowName = true };
        settings.Layouts[TicketType.SeasonPass] = custom;
        var handler = new GetTicketPrintSettings.Handler(settings);

        Assert.Same(custom, await handler.HandleAsync(new GetTicketPrintSettings.Query(TicketType.SeasonPass)));
        Assert.Same(TicketPrintLayout.Default, await handler.HandleAsync(new GetTicketPrintSettings.Query(TicketType.EventTicket)));
    }
}
