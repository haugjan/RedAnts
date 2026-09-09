namespace RedAnts.Ticketing.Features.Tickets;

public sealed record EventTicketMailDefaults(string Subject, string Body);

public static class GetEventTicketMailDefaults
{
    public sealed record Query;

    public sealed class Handler(IEventTicketMailer mailer)
    {
        public Task<EventTicketMailDefaults> HandleAsync(Query query) =>
            Task.FromResult(new EventTicketMailDefaults(mailer.DefaultSubject, mailer.DefaultBody));
    }
}
