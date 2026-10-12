using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;

namespace RedAnts.Show.Features.Board.Infrastructure;

public sealed class AgentDeckMirror(IHubContext<AgentHub> hub, IAgentConnections connections, ILogger<AgentDeckMirror> log) : IDeckMirror
{
    private const char IconSeparator = '|';

    private static readonly TimeSpan ViewTimeout = TimeSpan.FromSeconds(4);

    private readonly ConcurrentDictionary<string, (long Version, DeckView View)> _known = new();

    public bool IsConnected(string hallId) => connections.IsConnected(hallId);

    public async Task<DeckView?> LiveAsync(string hallId, DeckLayout layout)
    {
        if (connections.Of(hallId) is not { } agent) return null;
        var cacheKey = $"{agent.ConnectionId}|{layout.Id}";
        var known = _known.TryGetValue(cacheKey, out var cached) ? cached : default;
        using var timeout = new CancellationTokenSource(ViewTimeout);
        try
        {
            var answer = await hub.Clients.Client(agent.ConnectionId)
                .InvokeAsync<JsonElement>("ViewSince", layout.Id, known.Version, timeout.Token);
            var view = Read(answer, layout, known.View);
            if (view is not null) _known[cacheKey] = (Version(answer), view);
            return view;
        }
        catch (Exception e) when (e is not OperationCanceledException || timeout.IsCancellationRequested)
        {
            log.LogDebug("Deck {Deck} von Halle {Hall} nicht abrufbar: {Message}", layout.Id, hallId, e.Message);
            return null;
        }
    }

    public async Task<bool> PressAsync(string hallId, string deckId, int number)
    {
        if (!connections.IsConnected(hallId)) return false;
        await hub.Clients.Group(AgentHub.GroupOf(hallId)).SendAsync("Press", deckId, number);
        return true;
    }

    private static long Version(JsonElement view) =>
        view.TryGetProperty("version", out var value) && value.TryGetInt64(out var version) ? version : 0;

    private static DeckView? Read(JsonElement view, DeckLayout layout, DeckView? known)
    {
        if (view.ValueKind != JsonValueKind.Object) return null;
        var live = LiveOf(view);
        var states = StatesOf(view);
        if (!view.TryGetProperty("slots", out var slots) || slots.ValueKind != JsonValueKind.Array)
            return known is null ? null : known with { Live = live, States = states };

        var keys = slots.EnumerateArray().Select(slot => new DeckKeyRow(
            slot.GetProperty("number").GetInt32(),
            slot.TryGetProperty("label", out var label) ? label.GetString() ?? "" : "",
            slot.TryGetProperty("image", out var image) ? IconOf(image.GetString()) : null,
            Hex(slot, "bgcolor"),
            Hex(slot, "color"),
            slot.TryGetProperty("pressable", out var pressable) && pressable.GetBoolean(),
            null)).ToList();
        var title = view.TryGetProperty("title", out var text) ? text.GetString() ?? layout.Name : layout.Name;
        return new DeckView(layout.Id, title, layout.Columns, keys, true, live, states);
    }

    private static DeckLive? LiveOf(JsonElement view)
    {
        if (!view.TryGetProperty("live", out var live) || live.ValueKind != JsonValueKind.Object) return null;
        string Text(string name) => live.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";
        return new DeckLive(
            Text("time"),
            Text("periodLabel"),
            Text("home"),
            Text("away"),
            live.TryGetProperty("clock", out var running) && running.ValueKind == JsonValueKind.True);
    }

    private static IReadOnlyDictionary<string, string>? StatesOf(JsonElement view)
    {
        if (!view.TryGetProperty("states", out var states) || states.ValueKind != JsonValueKind.Object) return null;
        return states.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? "", StringComparer.OrdinalIgnoreCase);
    }

    private static string? IconOf(string? imageKey)
    {
        if (imageKey is not { Length: > 0 }) return null;
        var parts = imageKey.Split(IconSeparator);
        return parts.Length == 3 && parts[0].Length > 0 ? parts[0] : null;
    }

    private static string Hex(JsonElement slot, string property) =>
        slot.TryGetProperty(property, out var value) && value.TryGetInt32(out var color)
            ? "#" + color.ToString("X6")
            : "#000000";
}
