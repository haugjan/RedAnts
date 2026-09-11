namespace RedAnts.Show.Features.Remote;

public static class GetBoardView
{
    private static readonly TimeSpan LongPoll = TimeSpan.FromSeconds(25);

    public sealed record Query(string? Room, long? Since = null);

    public sealed class Handler(IShowBoardViews views)
    {
        public Task<PublishedBoardView?> HandleAsync(Query query, CancellationToken cancellation = default) =>
            query.Since is { } since
                ? views.WaitForChangeAsync(query.Room, since, LongPoll, cancellation)
                : Task.FromResult(views.Current(query.Room));
    }
}
