using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CatalogWorkflow;

public static class SetEventSalesStatus
{
    public sealed record Command(int EventId, EventStatus Status);

    public sealed class Handler(IEventStatusPublisher status)
    {
        public Task HandleAsync(Command command) => status.SetStatusAsync(command.EventId, command.Status);
    }
}
