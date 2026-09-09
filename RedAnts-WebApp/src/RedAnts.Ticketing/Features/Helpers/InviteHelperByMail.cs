using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Email;

namespace RedAnts.Ticketing.Features.Helpers;

public static class InviteHelperByMail
{
    public sealed record Command(Helper Helper, string Subject, string Body, string LoginLink);

    public sealed class Handler(IHelperInviteMailer mailer)
    {
        public Task<EmailSendResult> HandleAsync(Command command) =>
            mailer.SendAsync(command.Helper, command.Subject, command.Body, command.LoginLink);
    }
}
