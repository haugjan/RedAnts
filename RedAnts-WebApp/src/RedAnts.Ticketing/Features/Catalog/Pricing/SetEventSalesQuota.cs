using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog.Pricing;

public static class SetEventSalesQuota
{
    public sealed record Command(int EventId, int? Quota);

    public sealed class Handler(IEventPriceRepository prices)
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
