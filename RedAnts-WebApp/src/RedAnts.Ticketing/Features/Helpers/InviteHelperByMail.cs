using RedAnts.Ticketing.Features.Email;

namespace RedAnts.Ticketing.Features.Helpers;

public static class InviteHelperByMail
{
    public const string NotFound = "Helfer wurde nicht gefunden.";

    public sealed record Command(int HelperId, string Subject, string Body, string LoginLink);

    public sealed class Handler(IHelperRepository helpers, IHelperInviteMailer mailer)
    {
        public async Task<EmailSendResult> HandleAsync(Command command)
        {
            var helper = await helpers.FindByIdAsync(command.HelperId);
            if (helper is null) return new EmailSendResult(false, NotFound);
            return await mailer.SendAsync(helper, command.Subject, command.Body, command.LoginLink);
        }
    }
}
