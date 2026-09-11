using System.Collections.Concurrent;
using System.Text.Json;
using RedAnts.Show.Features.Admin;

namespace RedAnts.Show.Features.Remote.Infrastructure;

public sealed class ShowRooms(IShowSettings settings) : IShowRooms
{
    public const string SettingKey = "Show:Rooms";
    private const int MaxNameLength = 40;

    private readonly ConcurrentDictionary<string, DateTimeOffset> _seen = new();
    private readonly SemaphoreSlim _write = new(1, 1);

    public IReadOnlyList<ShowRoom> All() =>
        Stored().Select(name => new ShowRoom(name, _seen.TryGetValue(name, out var at) ? at : null)).ToList();

    public async Task RecordAsync(string room)
    {
        if (Normalize(room) is not { } name) return;
        _seen[name] = SwissTime.Timestamp;
        if (Stored().Contains(name)) return;
        await ChangeAsync(names => names.Add(name));
    }

    public async Task DeleteAsync(string room)
    {
        if (Normalize(room) is not { } name) return;
        _seen.TryRemove(name, out _);
        await ChangeAsync(names => names.Remove(name));
    }

    private async Task ChangeAsync(Func<SortedSet<string>, bool> change)
    {
        await _write.WaitAsync();
        try
        {
            var names = new SortedSet<string>(Stored(), StringComparer.Ordinal);
            if (change(names)) await settings.SetAsync(SettingKey, JsonSerializer.Serialize(names));
        }
        finally
        {
            _write.Release();
        }
    }

    private List<string> Stored()
    {
        var json = settings.Get(SettingKey);
        if (json is null) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? Normalize(string room)
    {
        var name = room.Trim().ToLowerInvariant();
        return name.Length is > 0 and <= MaxNameLength ? name : null;
    }
}
