using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog.Pricing;

public static class SetEventAdmissionQuota
{
    public sealed record Command(int EventId, int? Quota);

    public sealed class Handler(IEventPriceRepository prices)
    {
        public async Task HandleAsync(Command command)
        {
            var existing = await prices.GetByEventAsync(command.EventId);
            var updated = existing?.WithAdmissionQuota(command.Quota)
                ?? EventPrice.Create(command.EventId, null, command.Quota, []);
            await prices.SaveAsync(updated);
        }
    }
}
