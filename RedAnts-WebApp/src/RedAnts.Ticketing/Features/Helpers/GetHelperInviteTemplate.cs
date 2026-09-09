using RedAnts.Ticketing.Features.Shared;

namespace RedAnts.Ticketing.Features.Helpers;

public static class GetHelperInviteTemplate
{
    public sealed record Query;

    public sealed class Handler(IHelperInviteMailer mailer)
    {
        public Task<MailTemplate> HandleAsync(Query query) =>
            Task.FromResult(new MailTemplate(mailer.DefaultSubject, mailer.DefaultBody));
    }
}
