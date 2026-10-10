using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Agents;

public interface IAgentReports
{
    Task RecordAsync(string hallId, AgentReport report);
    Task<IReadOnlyList<AgentReport>> LatestAsync(string hallId);
}
