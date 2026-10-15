using System.Collections.Concurrent;

namespace RedAnts.Show.Features.Board;

public sealed class ShowBoardFeed
{
    private readonly ConcurrentDictionary<string, DeckView> _views = new(StringComparer.OrdinalIgnoreCase);

    public event Action<string>? Changed;

    public DeckView? Of(string hallId, string deckId) =>
        _views.TryGetValue(Key(hallId, deckId), out var view) ? view : null;

    public void Apply(string hallId, string deckId, DeckView view)
    {
        _views[Key(hallId, deckId)] = view;
        Changed?.Invoke(hallId);
    }

    public void ApplyLive(string hallId, DeckLive? live, IReadOnlyDictionary<string, string>? states)
    {
        foreach (var entry in _views.Where(e => e.Key.StartsWith(hallId + "|", StringComparison.OrdinalIgnoreCase)).ToList())
            _views[entry.Key] = entry.Value with { Live = live ?? entry.Value.Live, States = states ?? entry.Value.States };
        Changed?.Invoke(hallId);
    }

    public void Drop(string hallId)
    {
        foreach (var key in _views.Keys.Where(k => k.StartsWith(hallId + "|", StringComparison.OrdinalIgnoreCase)).ToList())
            _views.TryRemove(key, out _);
        Changed?.Invoke(hallId);
    }

    private static string Key(string hallId, string deckId) => hallId + "|" + deckId;
}
