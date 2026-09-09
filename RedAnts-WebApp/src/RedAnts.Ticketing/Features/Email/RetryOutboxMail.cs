namespace RedAnts.Ticketing.Features.Email;

public static class RetryOutboxMail
{
    public sealed record Command(int Id);

    public sealed class Handler(IEmailOutbox outbox)
    {
        public Task<bool> HandleAsync(Command command) => outbox.RequeueAsync(command.Id);
    }
}
