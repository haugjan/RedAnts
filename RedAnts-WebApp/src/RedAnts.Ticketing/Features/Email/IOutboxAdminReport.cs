namespace RedAnts.Ticketing.Features.Email;

public interface IOutboxAdminReport
{
    Task<IReadOnlyList<OutboxEntry>> ListAsync(bool includeSent, DateTimeOffset sentSince);
    Task<bool> RequeueAsync(int id);
}
