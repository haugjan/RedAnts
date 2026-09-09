using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Ports;

public interface INewsletterSignups
{
    Task SubscribeAsync(string email, string? name, string source);
    Task<IReadOnlyList<NewsletterSignup>> GetAllAsync();
    Task<IReadOnlyList<NewsletterSignup>> GetPendingAsync();
    Task SetTransferStatusAsync(int id, NewsletterTransferStatus status);
    Task MarkTransferredAsync(IEnumerable<int> ids);
}
