using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

namespace RedAnts.Show.Features.Agents.Infrastructure;

public sealed class AgentChannel(IHubContext<AgentHub> hub, IAgentConnections connections, ILogger<AgentChannel> log) : IAgentChannel
{
    private static readonly TimeSpan ViewTimeout = TimeSpan.FromSeconds(4);

    public Task SyncAsync(string hallId, AgentPayload payload) =>
        hub.Clients.Group(AgentHub.GroupOf(hallId)).SendAsync("Sync", payload);

    public Task RunAsync(string hallId, string sceneId, IReadOnlyDictionary<string, string> args) =>
        hub.Clients.Group(AgentHub.GroupOf(hallId)).SendAsync("Run", sceneId, args);

    public Task PanicAsync(string hallId) =>
        hub.Clients.Group(AgentHub.GroupOf(hallId)).SendAsync("Panic");

    public Task PressAsync(string hallId, string deckId, int number) =>
        hub.Clients.Group(AgentHub.GroupOf(hallId)).SendAsync("Press", deckId, number);

    public async Task<JsonElement?> ViewAsync(string hallId, string deckId)
    {
        if (connections.Of(hallId) is not { } agent) return null;
        using var timeout = new CancellationTokenSource(ViewTimeout);
        try
        {
            return await hub.Clients.Client(agent.ConnectionId).InvokeAsync<JsonElement>("View", deckId, timeout.Token);
        }
        catch (Exception e) when (e is not OperationCanceledException || timeout.IsCancellationRequested)
        {
            log.LogDebug("Deck {Deck} von Halle {Hall} nicht abrufbar: {Message}", deckId, hallId, e.Message);
            return null;
        }
    }
}
