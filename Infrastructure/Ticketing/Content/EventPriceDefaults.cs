using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Content;

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
                .Where(c => c.TicketOffered)
                .Select(c => CategoryPrice.Create(c.Category, c.TicketPrice, c.TicketQuota, c.TicketAvailableUntil))
                .ToList();
            if (categories.Count == 0 && seasonDefaults.TotalSalesQuota is null && seasonDefaults.DefaultTicketSalesQuota is null) continue;

            await eventPrices.SaveAsync(EventPrice.Create(entity.Id, seasonDefaults.DefaultTicketSalesQuota, seasonDefaults.TotalSalesQuota, categories));
        }
    }
}
