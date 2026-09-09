using RedAnts.Ticketing.Features.Email;

namespace RedAnts.Ticketing.Features.MemberCards;

public static class SendMemberCardMail
{
    public sealed record Command(Guid Uuid, string Subject, string Body);

    public sealed class Handler(IMemberCardRepository cards, IMemberCardMailer mailer)
    {
        public async Task<EmailSendResult> HandleAsync(Command command)
        {
            var card = await cards.GetByUuidAsync(command.Uuid);
            if (card is null) return new EmailSendResult(false, EditMemberCard.NotFound);
            return await mailer.SendAsync(card, command.Subject, command.Body);
        }
    }
}
