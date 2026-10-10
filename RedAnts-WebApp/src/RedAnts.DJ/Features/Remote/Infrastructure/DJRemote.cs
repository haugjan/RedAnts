using RedAnts.DJ.Domain;

namespace RedAnts.DJ.Features.Remote.Infrastructure;

public sealed class DJRemote : IDJRemote
{
    private readonly List<Entry> _handlers = new();
    private readonly object _lock = new();

    public IDisposable Register(string? room, Func<DJCommand, Task> handler)
    {
        var entry = new Entry(Normalize(room), handler);
        lock (_lock) _handlers.Add(entry);
        return new Registration(this, entry);
    }

    public async Task<int> DispatchAsync(DJCommand command)
    {
        var room = Normalize(command.Room);
        Entry[] handlers;
        lock (_lock) handlers = _handlers.ToArray();
        var reached = 0;
        foreach (var h in handlers)
        {
            if (room is not null && h.Room != room) continue;
            try { await h.Handler(command); reached++; } catch { }
        }
        return reached;
    }

    private static string? Normalize(string? room) =>
        string.IsNullOrWhiteSpace(room) ? null : room.Trim().ToLowerInvariant();

    private void Remove(Entry entry)
    {
        lock (_lock) _handlers.Remove(entry);
    }

    private sealed record Entry(string? Room, Func<DJCommand, Task> Handler);

    private sealed class Registration(DJRemote owner, Entry entry) : IDisposable
    {
        public void Dispose() => owner.Remove(entry);
    }
}
