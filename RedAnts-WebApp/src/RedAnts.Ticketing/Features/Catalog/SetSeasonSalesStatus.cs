using RedAnts.Ticketing.Domain;

namespace RedAnts.Ticketing.Features.Catalog;

public static class SetSeasonSalesStatus
{
    public sealed record Command(int SeasonId, SeasonStatus Status);

    public sealed class Handler(ISeasonStatusPublisher status)
    {
        public Task HandleAsync(Command command) => status.SetStatusAsync(command.SeasonId, command.Status);
    }
}
