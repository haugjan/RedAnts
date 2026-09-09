using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Email;

namespace RedAnts.Ticketing.Features.MemberCards;

public static class SendMemberCardMail
{
    public sealed record Command(MemberCard Card, string Subject, string Body);

    public sealed class Handler(IMemberCardMailer mailer)
    {
        public Task<EmailSendResult> HandleAsync(Command command) => mailer.SendAsync(command.Card, command.Subject, command.Body);
    }
}
