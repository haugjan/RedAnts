namespace RedAnts.Show.Features.Agents;

public interface IAgentChannel
{
    Task SyncAsync(string hallId, AgentPayload payload);
    Task PanicAsync(string hallId);
}
