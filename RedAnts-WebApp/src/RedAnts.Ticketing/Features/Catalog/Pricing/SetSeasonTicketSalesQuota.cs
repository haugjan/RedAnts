using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog.Pricing;

public static class SetSeasonTicketSalesQuota
{
    public sealed record Command(int SeasonId, int? Quota);

    public sealed class Handler(ISeasonPriceRepository prices)
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
