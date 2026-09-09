namespace RedAnts.Ticketing.Features.Newsletter;

public static class GetNewsletterSignups
{
    public sealed record Query(bool OnlyPending = false);

    public sealed class Handler(INewsletterSignupListReader signups)
    {
        public Task<IReadOnlyList<NewsletterSignupRow>> HandleAsync(Query query) =>
            query.OnlyPending ? signups.GetPendingAsync() : signups.GetAllAsync();
    }
}
