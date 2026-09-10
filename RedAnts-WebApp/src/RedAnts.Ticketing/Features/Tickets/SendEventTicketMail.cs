using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Email;

namespace RedAnts.Ticketing.Features.Tickets;

public static class SendEventTicketMail
{
    public sealed record Command(Guid Uuid, string Subject, string Body);

    public sealed class Handler(IEventTicketRepository tickets, IEventTicketMailer mailer)
    {
        public async Task<EmailSendResult> HandleAsync(Command command)
        {
            var ticket = await tickets.GetByUuidAsync(command.Uuid)
                ?? throw new DomainException("Ticket wurde nicht gefunden.");
            var mailTicket = new EventMailTicket(ticket.Uuid, ticket.Holder?.Email?.Value, ticket.Holder?.DisplayName, ticket.EventId,
                ticket.Category.DisplayName());
            return await mailer.SendAsync(mailTicket, command.Subject, command.Body);
        }
    }
}
