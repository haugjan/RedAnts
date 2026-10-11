using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Agents;

public static class RecordAgentReport
{
    public sealed record Command(string HallId, string Kind, string Json);

    public sealed class Handler(IAgentReports reports)
    {
        public Task HandleAsync(Command command)
        {
            ValidationException.ThrowIfBlank(command.Kind, nameof(command.Kind), "Art der Meldung");
            return reports.RecordAsync(command.HallId, new AgentReport(command.Kind.Trim().ToLowerInvariant(), command.Json, SwissTime.Timestamp));
        }
    }
}
