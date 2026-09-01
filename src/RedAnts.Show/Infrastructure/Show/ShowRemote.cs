namespace RedAnts.Infrastructure.Show;

public sealed record ShowCommand(string Action, string? TileId = null, string? ProfileId = null, int? SongIndex = null);

public interface IShowRemote
{
    IDisposable Register(Func<ShowCommand, Task> handler);
    Task<int> DispatchAsync(ShowCommand cmd);
}

// Bridge between the HTTP control API and the connected Blazor board circuit(s).
public sealed class ShowRemote : IShowRemote
{
    private readonly List<Func<ShowCommand, Task>> _handlers = new();
    private readonly object _lock = new();

    public IDisposable Register(Func<ShowCommand, Task> handler)
    {
        lock (_lock) _handlers.Add(handler);
        return new Registration(this, handler);
    }

    public async Task<int> DispatchAsync(ShowCommand cmd)
    {
        Func<ShowCommand, Task>[] handlers;
        lock (_lock) handlers = _handlers.ToArray();
        var reached = 0;
        foreach (var h in handlers)
        {
            try { await h(cmd); reached++; }
            catch { }
        }
        return reached;
    }

    private void Remove(Func<ShowCommand, Task> handler)
    {
        lock (_lock) _handlers.Remove(handler);
    }

    private sealed class Registration(ShowRemote owner, Func<ShowCommand, Task> handler) : IDisposable
    {
        public void Dispose() => owner.Remove(handler);
    }
}
