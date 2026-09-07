using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Admin;

namespace RedAnts.Features.Ticketing.CatalogWorkflow;

public static class SetEventSalesStatus
{
    public sealed record Command(int EventId, EventStatus Status);

    public sealed class Handler(IEventStatusEditor status)
    {
        public Task HandleAsync(Command command) => status.SetStatusAsync(command.EventId, command.Status);
    }
}
