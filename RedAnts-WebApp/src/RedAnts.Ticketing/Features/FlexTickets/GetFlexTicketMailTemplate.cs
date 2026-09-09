using RedAnts.Ticketing.Features.Shared;

namespace RedAnts.Ticketing.Features.FlexTickets;

public static class GetFlexTicketMailTemplate
{
    public sealed record Query;

    public sealed class Handler(IFlexTicketMailer mailer)
    {
        public Task<MailTemplate> HandleAsync(Query query) =>
            Task.FromResult(new MailTemplate(mailer.DefaultSubject, mailer.DefaultBody));
    }
}
