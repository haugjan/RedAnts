using Microsoft.AspNetCore.SignalR;

namespace RedAnts.Show.Features.Agents.Infrastructure;

public sealed class AgentChannel(IHubContext<AgentHub> hub) : IAgentChannel
{
    public Task SyncAsync(string hallId, AgentPayload payload) =>
        hub.Clients.Group(AgentHub.GroupOf(hallId)).SendAsync("Sync", payload);


    public Task PanicAsync(string hallId) =>
        hub.Clients.Group(AgentHub.GroupOf(hallId)).SendAsync("Panic");
}
