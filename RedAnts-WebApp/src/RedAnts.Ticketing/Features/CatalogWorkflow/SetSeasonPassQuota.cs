using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CatalogWorkflow;

public static class SetSeasonPassQuota
{
    public sealed record Command(int SeasonId, int? Quota);

    public sealed class Handler(ISeasonPrices prices)
    {
        public async Task HandleAsync(Command command)
        {
            var existing = await prices.GetBySeasonAsync(command.SeasonId);
            var price = existing?.WithTotalSalesQuota(command.Quota)
                ?? SeasonPrice.Create(command.SeasonId, command.Quota, []);
            await prices.SaveAsync(price);
        }
    }
}
