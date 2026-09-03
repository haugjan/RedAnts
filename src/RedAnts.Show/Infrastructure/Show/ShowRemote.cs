namespace RedAnts.Infrastructure.Show;

public sealed record ShowCommand(string Action, string? TileId = null, string? ProfileId = null, int? SongIndex = null, string? Room = null);

public interface IShowRemote
{
    IDisposable Register(string? room, Func<ShowCommand, Task> handler);
    Task<int> DispatchAsync(ShowCommand cmd);
}

public sealed class ShowRemote : IShowRemote
{
    private readonly List<Entry> _handlers = new();
    private readonly object _lock = new();

    public IDisposable Register(string? room, Func<ShowCommand, Task> handler)
    {
        var entry = new Entry(Normalize(room), handler);
        lock (_lock) _handlers.Add(entry);
        return new Registration(this, entry);
    }

    public async Task<int> DispatchAsync(ShowCommand cmd)
    {
        var room = Normalize(cmd.Room);
        Entry[] handlers;
        lock (_lock) handlers = _handlers.ToArray();
        var reached = 0;
        foreach (var h in handlers)
        {
            if (room is not null && h.Room != room) continue;
            try { await h.Handler(cmd); reached++; } catch { }
        }
        return reached;
    }

    private static string? Normalize(string? room) =>
        string.IsNullOrWhiteSpace(room) ? null : room.Trim().ToLowerInvariant();

    private void Remove(Entry entry)
    {
        lock (_lock) _handlers.Remove(entry);
    }

    private sealed record Entry(string? Room, Func<ShowCommand, Task> Handler);

    private sealed class Registration(ShowRemote owner, Entry entry) : IDisposable
    {
        public void Dispose() => owner.Remove(entry);
    }
}
