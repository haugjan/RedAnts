using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admission;
using RedAnts.Ticketing.Features.Tickets;
using RedAnts.Ticketing.Tests.Checkout;
using RedAnts.Ticketing.Tests.Tickets;
using Xunit;

namespace RedAnts.Ticketing.Tests.Admission;

public class ScannerTestCardsTests
{
    [Fact]
    public async Task Renders_one_example_card_per_ticket_type_with_a_qr_for_the_public_ticket_url()
    {
        var handler = new GetScannerTestCards.Handler(new StubTicketTokens(), new StubQr(), new StubPublicBaseUrl());

        var cards = await handler.HandleAsync(new GetScannerTestCards.Query());

        Assert.Equal(4, cards.Count);
        Assert.All(cards, c => Assert.StartsWith("svg:https://tickets.test/ticket/full-", c.QrMarkup));
        Assert.Equal("Erika Muster", cards.Single(c => c.TypeLabel == TicketDisplay.TypeLabel(TicketType.MemberCard)).HolderName);
    }
}
