using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CatalogWorkflow;

public static class SetEventSalesQuota
{
    public sealed record Command(int EventId, int? Quota);

    public sealed class Handler(IEventPrices prices)
    {
        public async Task HandleAsync(Command command)
        {
            var existing = await prices.GetByEventAsync(command.EventId);
            var updated = existing?.WithSalesQuota(command.Quota)
                ?? EventPrice.Create(command.EventId, command.Quota, null, []);
            await prices.SaveAsync(updated);
        }
    }
}
