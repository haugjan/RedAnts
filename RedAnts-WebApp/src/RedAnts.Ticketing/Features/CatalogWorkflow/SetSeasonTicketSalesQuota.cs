using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CatalogWorkflow;

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
