namespace RedAnts.Ticketing.Features.Newsletter;

public interface INewsletterSignupListReader
{
    Task<IReadOnlyList<NewsletterSignupRow>> GetAllAsync();
    Task<IReadOnlyList<NewsletterSignupRow>> GetPendingAsync();
}
