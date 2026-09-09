using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Newsletter;

public static class SetNewsletterTransferStatus
{
    public sealed record Command(int Id, NewsletterTransferStatus Status);

    public sealed class Handler(INewsletterSignupRepository signups)
    {
        public Task HandleAsync(Command command) => signups.SetTransferStatusAsync(command.Id, command.Status);
    }
}
