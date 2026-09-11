namespace RedAnts.Show.Features.Remote.Infrastructure;

public sealed class ShowBoardViews : IShowBoardViews
{
    private readonly Dictionary<Guid, Entry> _boards = new();
    private readonly object _lock = new();
    private TaskCompletionSource _changed = NewSignal();
    private long _version;

    public void Publish(Guid boardId, string? room, ShowBoardView view)
    {
        var normalized = Normalize(room);
        TaskCompletionSource signal;
        lock (_lock)
        {
            if (_boards.TryGetValue(boardId, out var existing) && existing.Room == normalized && existing.Published.View.SameAs(view)) return;
            _boards[boardId] = new Entry(normalized, new PublishedBoardView(++_version, view));
            signal = SwapSignal();
        }
        signal.TrySetResult();
    }

    public void Withdraw(Guid boardId)
    {
        TaskCompletionSource signal;
        lock (_lock)
        {
            if (!_boards.Remove(boardId)) return;
            _version++;
            signal = SwapSignal();
        }
        signal.TrySetResult();
    }

    public PublishedBoardView? Current(string? room)
    {
        var normalized = Normalize(room);
        lock (_lock)
        {
            return _boards.Values
                .Where(e => normalized is null || e.Room == normalized)
                .Select(e => e.Published)
                .MaxBy(p => p.Version);
        }
    }

    public async Task<PublishedBoardView?> WaitForChangeAsync(string? room, long since, TimeSpan timeout, CancellationToken cancellation)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        limit.CancelAfter(timeout);
        while (true)
        {
            Task signal;
            lock (_lock) signal = _changed.Task;
            var current = Current(room);
            if ((current?.Version ?? 0) != since) return current;
            try
            {
                await signal.WaitAsync(limit.Token);
            }
            catch (OperationCanceledException)
            {
                cancellation.ThrowIfCancellationRequested();
                return Current(room);
            }
        }
    }

    private TaskCompletionSource SwapSignal()
    {
        var previous = _changed;
        _changed = NewSignal();
        return previous;
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static string? Normalize(string? room) =>
        string.IsNullOrWhiteSpace(room) ? null : room.Trim().ToLowerInvariant();

    private sealed record Entry(string? Room, PublishedBoardView Published);
}
