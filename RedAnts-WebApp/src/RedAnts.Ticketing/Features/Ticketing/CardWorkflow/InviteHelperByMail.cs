using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Email;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class InviteHelperByMail
{
    public sealed record Command(Helper Helper, string Subject, string Body, string LoginLink);

    public sealed class Handler(IHelperInviteMailer mailer)
    {
        public Task<EmailSendResult> HandleAsync(Command command) =>
            mailer.SendAsync(command.Helper, command.Subject, command.Body, command.LoginLink);
    }
}
