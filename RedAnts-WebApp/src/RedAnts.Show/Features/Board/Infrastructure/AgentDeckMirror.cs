using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;

namespace RedAnts.Show.Features.Board.Infrastructure;

public sealed class AgentDeckMirror(IHubContext<AgentHub> hub, IAgentConnections connections, ILogger<AgentDeckMirror> log) : IDeckMirror
{
    private static readonly TimeSpan ViewTimeout = TimeSpan.FromSeconds(4);

    public bool IsConnected(string hallId) => connections.IsConnected(hallId);

    public async Task<DeckView?> LiveAsync(string hallId, DeckLayout layout)
    {
        if (connections.Of(hallId) is not { } agent) return null;
        using var timeout = new CancellationTokenSource(ViewTimeout);
        try
        {
            var view = await hub.Clients.Client(agent.ConnectionId).InvokeAsync<JsonElement>("View", layout.Id, timeout.Token);
            return Read(view, layout);
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

    private static DeckView? Read(JsonElement view, DeckLayout layout)
    {
        if (view.ValueKind != JsonValueKind.Object) return null;
        if (!view.TryGetProperty("slots", out var slots) || slots.ValueKind != JsonValueKind.Array) return null;
        var keys = slots.EnumerateArray().Select(slot => new DeckKeyRow(
            slot.GetProperty("number").GetInt32(),
            slot.TryGetProperty("label", out var label) ? label.GetString() ?? "" : "",
            slot.TryGetProperty("image", out var image) ? IconOf(image.GetString()) : null,
            Hex(slot, "bgcolor"),
            Hex(slot, "color"),
            slot.TryGetProperty("pressable", out var pressable) && pressable.GetBoolean(),
            null)).ToList();
        var title = view.TryGetProperty("title", out var text) ? text.GetString() ?? layout.Name : layout.Name;
        return new DeckView(layout.Id, title, layout.Columns, keys, true);
    }

    private static string? IconOf(string? imageKey)
    {
        if (imageKey is not { Length: > 0 }) return null;
        var parts = imageKey.Split(IconSeparator);
        return parts.Length == 3 && parts[0].Length > 0 ? parts[0] : null;
    }

    private const char IconSeparator = '|';

    private static string Hex(JsonElement slot, string property) =>
        slot.TryGetProperty(property, out var value) && value.TryGetInt32(out var color)
            ? "#" + color.ToString("X6")
            : "#000000";
}
