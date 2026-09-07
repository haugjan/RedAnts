using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CatalogWorkflow;

public static class SetSeasonTicketSalesQuota
{
    public sealed record Command(int SeasonId, int? Quota);

    public sealed class Handler(ISeasonPrices prices)
    {
        public async Task HandleAsync(Command command)
        {
            var existing = await prices.GetBySeasonAsync(command.SeasonId);
            var price = existing?.WithDefaultTicketSalesQuota(command.Quota)
                ?? SeasonPrice.Create(command.SeasonId, null, [], command.Quota);
            await prices.SaveAsync(price);
        }
    }
}
