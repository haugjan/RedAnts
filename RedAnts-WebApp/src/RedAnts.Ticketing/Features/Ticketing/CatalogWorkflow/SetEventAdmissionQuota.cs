using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CatalogWorkflow;

public static class SetEventAdmissionQuota
{
    public sealed record Command(int EventId, int? Quota);

    public sealed class Handler(IEventPrices prices)
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
