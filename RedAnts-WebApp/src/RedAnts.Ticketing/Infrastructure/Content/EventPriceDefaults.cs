using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using A = RedAnts.Ticketing.Infrastructure.Content.TicketingAliases;

namespace RedAnts.Ticketing.Infrastructure.Content;

public sealed class EventPriceDefaultsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<ContentSavedNotification, EventPriceDefaults>();
}

public sealed class EventPriceDefaults(
    IEventPrices eventPrices,
    ISeasonPrices seasonPrices) : INotificationAsyncHandler<ContentSavedNotification>
{
    public async Task HandleAsync(ContentSavedNotification notification, CancellationToken cancellationToken)
    {
        foreach (var entity in notification.SavedEntities.Where(e => e.ContentType.Alias == A.EventType))
        {
            if (await eventPrices.GetByEventAsync(entity.Id) is not null) continue;

            var seasonDefaults = await seasonPrices.GetBySeasonAsync(entity.ParentId);
            if (seasonDefaults is null) continue;

            var categories = seasonDefaults.Categories
                .Where(c => c.TicketOffered && c.TierId is not null)
                .Select(c => CategoryPrice.Create(c.Category, c.TicketPrice, c.TicketQuota, c.TicketAvailableUntil, c.TierId))
                .ToList();
            if (categories.Count == 0 && seasonDefaults.TotalSalesQuota is null && seasonDefaults.DefaultTicketSalesQuota is null) continue;

            await eventPrices.SaveAsync(EventPrice.Create(entity.Id, seasonDefaults.DefaultTicketSalesQuota, seasonDefaults.TotalSalesQuota, categories));
        }
    }
}
