using Microsoft.AspNetCore.SignalR;

namespace RedAnts.Show.Features.Agents.Infrastructure;

public sealed class AgentChannel(IHubContext<AgentHub> hub) : IAgentChannel
{
    public Task SyncAsync(string hallId, AgentPayload payload) =>
        hub.Clients.Group(AgentHub.GroupOf(hallId)).SendAsync("Sync", payload);

    public Task RunAsync(string hallId, string sceneId, IReadOnlyDictionary<string, string> args) =>
        hub.Clients.Group(AgentHub.GroupOf(hallId)).SendAsync("Run", sceneId, args);

    public Task PanicAsync(string hallId) =>
        hub.Clients.Group(AgentHub.GroupOf(hallId)).SendAsync("Panic");
}
