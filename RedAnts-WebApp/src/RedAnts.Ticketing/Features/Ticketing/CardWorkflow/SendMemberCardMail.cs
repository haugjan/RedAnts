using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Email;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class SendMemberCardMail
{
    public sealed record Command(MemberCard Card, string Subject, string Body);

    public sealed class Handler(IMemberCardMailer mailer)
    {
        public Task<EmailSendResult> HandleAsync(Command command) => mailer.SendAsync(command.Card, command.Subject, command.Body);
    }
}
