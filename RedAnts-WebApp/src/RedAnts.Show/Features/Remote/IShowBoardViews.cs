namespace RedAnts.Show.Features.Remote;

public interface IShowBoardViews
{
    void Publish(Guid boardId, string? room, ShowBoardView view);
    void Withdraw(Guid boardId);
    PublishedBoardView? Current(string? room);
    Task<PublishedBoardView?> WaitForChangeAsync(string? room, long since, TimeSpan timeout, CancellationToken cancellation);
}
