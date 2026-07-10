using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public interface INewsletterSignups
{
    Task SubscribeAsync(string email, string? name, string source);
    Task<IReadOnlyList<NewsletterSignup>> GetAllAsync();
    Task SetTransferStatusAsync(int id, NewsletterTransferStatus status);
}
