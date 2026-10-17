using System.Text.Json;

namespace RedAnts.Show.Agent.Cloud;

public sealed class DeckPublisher(Deck.Deck deck, CloudLink cloud, ILogger<DeckPublisher> log) : BackgroundService
{
    static readonly TimeSpan Coalesce = TimeSpan.FromMilliseconds(150);
    static readonly TimeSpan Idle = TimeSpan.FromSeconds(10);

    readonly Dictionary<string, string> _keys = new(StringComparer.OrdinalIgnoreCase);
    string _live = "";

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        var since = 0L;
        while (!stop.IsCancellationRequested)
        {
            await deck.WaitAsync(since, Idle, stop);
            since = deck.Version;
            try { await Task.Delay(Coalesce, stop); }
            catch (OperationCanceledException) { break; }
            try { await PublishAsync(stop); }
            catch (OperationCanceledException) { break; }
            catch (Exception e) { log.LogDebug("Deck nicht gemeldet: {Message}", e.Message); }
        }
    }

    async Task PublishAsync(CancellationToken stop)
    {
        if (!cloud.Connected) return;
        var pushed = false;
        foreach (var id in deck.Decks)
        {
            var shape = Shape(deck.Slots(id));
            if (_keys.TryGetValue(id, out var known) && known == shape) continue;
            _keys[id] = shape;
            await cloud.SendAsync("Deck", stop, id, JsonSerializer.Serialize(deck.View(id), PayloadJson.Options));
            pushed = true;
        }
        if (pushed) { _live = JsonSerializer.Serialize(deck.LivePayload(), PayloadJson.Options); return; }

        var live = JsonSerializer.Serialize(deck.LivePayload(), PayloadJson.Options);
        if (live == _live) return;
        _live = live;
        await cloud.SendAsync("Live", stop, live);
    }

    static string Shape(IReadOnlyList<Deck.DeckSlot> slots) =>
        string.Join('|', slots.Select(s => $"{s.Number};{s.Label};{s.ImageKey};{s.Color};{s.TextColor};{s.Pressable}"));
}
