namespace RedAnts.DJ.Features.Remote;

public interface IDJBoardViews
{
    void Publish(Guid boardId, string? room, DJBoardView view);
    void Withdraw(Guid boardId);
    PublishedBoardView? Current(string? room);
    Task<PublishedBoardView?> WaitForChangeAsync(string? room, long since, TimeSpan timeout, CancellationToken cancellation);
}
