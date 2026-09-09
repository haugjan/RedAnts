using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;
using RedAnts.Ticketing.Tests.Checkout;
using Xunit;

namespace RedAnts.Ticketing.Tests.Tickets;

public class SendEventTicketMailTests
{
    private readonly InMemoryEventTicketRepository _tickets = new();
    private readonly RecordingEventTicketMailer _mailer = new();

    private SendEventTicketMail.Handler Handler => new(_tickets, _mailer);

    [Fact]
    public async Task Sends_the_mail_to_the_holder_of_the_ticket()
    {
        var holder = CardHolder.Create(BuyerType.Private, null, null, "Anna", "Muster", null, "anna@example.ch", null, null, null, null, null, null);
        var ticket = EventTicket.FromPersistence(5, Guid.NewGuid(), 10, TicketCategory.Youth, 12m, 7, TicketStatus.Valid,
            SwissTime.Timestamp, false, null, null, null, null, null, null, null, holder);
        _tickets.Stored.Add(ticket);

        var result = await Handler.HandleAsync(new SendEventTicketMail.Command(ticket.Uuid, "Betreff", "Text"));

        Assert.True(result.Success);
        var sent = Assert.Single(_mailer.Sent);
        Assert.Equal(("anna@example.ch", "Anna Muster", 10, TicketCategory.Youth.DisplayName()),
            (sent.Ticket.Email, sent.Ticket.HolderName, sent.Ticket.EventId, sent.Ticket.CategoryLabel));
        Assert.Equal(("Betreff", "Text"), (sent.Subject, sent.Body));
    }

    [Fact]
    public async Task Mail_defaults_come_from_the_mailer()
    {
        var defaults = await new GetEventTicketMailDefaults.Handler(_mailer).HandleAsync(new GetEventTicketMailDefaults.Query());

        Assert.Equal(("Dein Ticket", "Hallo {Name}"), (defaults.Subject, defaults.Body));
    }

    [Fact]
    public async Task An_unknown_ticket_is_rejected()
    {
        await Assert.ThrowsAsync<DomainException>(() => Handler.HandleAsync(new SendEventTicketMail.Command(Guid.NewGuid(), "B", "T")));
        Assert.Empty(_mailer.Sent);
    }
}
