using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Email;

namespace RedAnts.Ticketing.Features.CardWorkflow;

public static class SendSeasonPassMail
{
    public sealed record Command(SeasonPass Pass, string CategoryLabel, string Subject, string Body);

    public sealed class Handler(ISeasonPassMailer mailer)
    {
        public Task<EmailSendResult> HandleAsync(Command command) =>
            mailer.SendAsync(command.Pass, command.CategoryLabel, command.Subject, command.Body);
    }
}
