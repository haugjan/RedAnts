using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CatalogWorkflow;

public sealed record EventCategoryPriceInput(int TierId, decimal Price, int? Quota, DateOnly? AvailableUntil);

public static class SetEventPricing
{
    public sealed record Command(int EventId, IReadOnlyList<EventCategoryPriceInput> Categories);

    public sealed class Handler(IEventPrices prices)
    {
        public async Task HandleAsync(Command command)
        {
            var categories = command.Categories
                .Select(c => CategoryPrice.Create(default, decimal.Round(c.Price, 2), c.Quota, c.AvailableUntil, c.TierId))
                .ToList();
            var existing = await prices.GetByEventAsync(command.EventId);
            var updated = existing?.WithCategories(categories)
                ?? EventPrice.Create(command.EventId, null, null, categories);
            await prices.SaveAsync(updated);
        }
    }
}
