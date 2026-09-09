using RedAnts.Ticketing.Domain;

namespace RedAnts.Ticketing.Features.Catalog;

public static class SetEventSalesStatus
{
    public sealed record Command(int EventId, EventStatus Status);

    public sealed class Handler(IEventStatusPublisher status)
    {
        public Task HandleAsync(Command command) => status.SetStatusAsync(command.EventId, command.Status);
    }
}
