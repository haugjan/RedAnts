namespace RedAnts.Ticketing.Features.Newsletter;

public static class MarkNewsletterSignupsTransferred
{
    public sealed record Command(IReadOnlyList<int> Ids);

    public sealed class Handler(INewsletterSignupRepository signups)
    {
        public Task HandleAsync(Command command) => signups.MarkTransferredAsync(command.Ids);
    }
}
