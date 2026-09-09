using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CatalogWorkflow;

public static class SetEventSalesStatus
{
    public sealed record Command(int EventId, EventStatus Status);

    public sealed class Handler(IEventStatusPublisher status)
    {
        public Task HandleAsync(Command command) => status.SetStatusAsync(command.EventId, command.Status);
    }
}
