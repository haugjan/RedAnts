using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Newsletter;

public interface INewsletterSignupRepository
{
    Task SubscribeAsync(string email, string? name, string source);
    Task SetTransferStatusAsync(int id, NewsletterTransferStatus status);
    Task MarkTransferredAsync(IEnumerable<int> ids);
}
