namespace RedAnts.Ticketing.Features.Email;

public interface INewsletterNotifier
{
    Task NotifyNewSignupAsync(string email, string? name, string source, CancellationToken cancellationToken = default);
}
