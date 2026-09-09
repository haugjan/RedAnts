using RedAnts.Ticketing.Features.Email;

namespace RedAnts.Ticketing.Features.CardWorkflow;

public static class SendFlexTicketMail
{
    public sealed record Command(FlexMailTicket Ticket, string Subject, string Body);

    public sealed class Handler(IFlexTicketMailer mailer)
    {
        public Task<EmailSendResult> HandleAsync(Command command) => mailer.SendAsync(command.Ticket, command.Subject, command.Body);
    }
}
