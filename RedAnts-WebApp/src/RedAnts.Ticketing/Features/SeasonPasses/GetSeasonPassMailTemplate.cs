using RedAnts.Ticketing.Features.Shared;

namespace RedAnts.Ticketing.Features.SeasonPasses;

public static class GetSeasonPassMailTemplate
{
    public sealed record Query;

    public sealed class Handler(ISeasonPassMailer mailer)
    {
        public Task<MailTemplate> HandleAsync(Query query) =>
            Task.FromResult(new MailTemplate(mailer.DefaultSubject, mailer.DefaultBody));
    }
}
