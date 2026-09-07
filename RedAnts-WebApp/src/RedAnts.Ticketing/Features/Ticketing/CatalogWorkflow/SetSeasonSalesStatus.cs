using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CatalogWorkflow;

public static class SetSeasonSalesStatus
{
    public sealed record Command(int SeasonId, SeasonStatus Status);

    public sealed class Handler(ISeasonStatusPublisher status)
    {
        public Task HandleAsync(Command command) => status.SetStatusAsync(command.SeasonId, command.Status);
    }
}
