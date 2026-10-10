namespace RedAnts.Show.Features.Agents;

public interface IAgentChannel
{
    Task SyncAsync(string hallId, AgentPayload payload);
    Task RunAsync(string hallId, string sceneId, IReadOnlyDictionary<string, string> args);
    Task PanicAsync(string hallId);
}
