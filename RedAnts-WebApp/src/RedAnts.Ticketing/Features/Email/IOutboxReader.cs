namespace RedAnts.Ticketing.Features.Email;

public interface IOutboxReader
{
    Task<IReadOnlyList<OutboxEntry>> ListAsync(bool includeSent, DateTimeOffset sentSince);
}
