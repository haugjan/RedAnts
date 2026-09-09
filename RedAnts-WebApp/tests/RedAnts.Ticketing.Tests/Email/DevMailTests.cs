using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Email;
using RedAnts.Ticketing.Features.Tickets;
using RedAnts.Ticketing.Tests.Admission;
using RedAnts.Ticketing.Tests.Checkout;
using RedAnts.Ticketing.Tests.Tickets;
using Xunit;

namespace RedAnts.Ticketing.Tests.Email;

public class DevMailTests
{
    [Fact]
    public async Task SendOrderMailSample_renders_without_a_recipient_and_sends_with_one()
    {
        var mailer = new RecordingOrderMailer();
        var handler = new SendOrderMailSample.Handler(mailer, new StubPublicBaseUrl());

        var preview = await handler.HandleAsync(new SendOrderMailSample.Command(null));
        var sent = await handler.HandleAsync(new SendOrderMailSample.Command("anna@example.com"));

        Assert.Null(preview.Sent);
        Assert.True(sent.Sent);
        Assert.Equal("anna@example.com", Assert.Single(mailer.Sent).ToEmail);
    }

    [Fact]
    public async Task SendTestMail_embeds_the_ticket_qr_when_a_ticket_is_given()
    {
        var uuid = Guid.NewGuid();
        var issued = new StubIssuedTickets();
        issued.Tickets[uuid] = new IssuedTicket(TicketType.EventTicket, uuid, 7, TicketCategory.Adult, TicketStatus.Valid, DateTimeOffset.UtcNow, "Anna");
        var events = new Orders.StubEvents();
        events.Events.Add(Orders.StubEvents.InSeason(7, 1));
        var sender = new RecordingEmailSender();
        var handler = new SendTestMail.Handler(sender, issued, new StubTicketTokens(), new StubQr(), events, new Stats.StubSeasons(), new StubPublicBaseUrl());

        var result = await handler.HandleAsync(new SendTestMail.Command("anna@example.com", uuid));

        Assert.True(result.Success);
        var mail = Assert.Single(sender.Sent);
        Assert.Equal("Dein Ticket – Spiel 7", mail.Subject);
        Assert.Contains("data:https://tickets.test/ticket/short-", mail.Html);
        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new SendTestMail.Command(" ", null)));
    }
}
