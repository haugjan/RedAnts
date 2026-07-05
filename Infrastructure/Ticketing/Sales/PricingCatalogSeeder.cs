using Microsoft.Extensions.Logging;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class PricingCatalogSeederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, PricingCatalogSeeder>();
}

public sealed class PricingCatalogSeeder(
    IEventPrices eventPrices,
    ISeasonPrices seasonPrices,
    IEventPricing pricing,
    IEventTickets eventTickets,
    IContentService contentService,
    IContentTypeService contentTypeService,
    ILogger<PricingCatalogSeeder> logger) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private static readonly (TicketCategory Category, decimal Price)[] BaseCategories =
    [
        (TicketCategory.Adult, 25m),
        (TicketCategory.Youth, 15m),
        (TicketCategory.Child, 10m),
    ];

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await EnsureEventPricesAsync();
            await EnsureSeasonPricesAsync();
            await EnsureDemoTicketsAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PricingCatalogSeeder failed.");
        }
    }

    private static List<CategoryPrice> BaseCategoryPrices() =>
        BaseCategories.Select(c => CategoryPrice.Create(c.Category, c.Price, null)).ToList();

    private async Task EnsureEventPricesAsync()
    {
        var eventType = contentTypeService.Get(A.EventType);
        if (eventType is null) return;

        foreach (var evt in contentService.GetPagedOfType(eventType.Id, 0, 1000, out _, null))
        {
            if (await eventPrices.GetByEventAsync(evt.Id) is not null) continue;

            await eventPrices.SaveAsync(EventPrice.Create(evt.Id, totalSalesQuota: null, admissionQuota: null, BaseCategoryPrices()));
            logger.LogInformation("PricingCatalogSeeder: seeded price set for event '{Name}'.", evt.Name);
        }
    }

    private async Task EnsureSeasonPricesAsync()
    {
        var seasonType = contentTypeService.Get(A.SeasonType);
        if (seasonType is null) return;

        foreach (var season in contentService.GetPagedOfType(seasonType.Id, 0, 1000, out _, null))
        {
            if (await seasonPrices.GetBySeasonAsync(season.Id) is not null) continue;

            await seasonPrices.SaveAsync(SeasonPrice.Create(season.Id, BaseCategoryPrices()));
            logger.LogInformation("PricingCatalogSeeder: seeded price set for season '{Name}'.", season.Name);
        }
    }

    private async Task EnsureDemoTicketsAsync()
    {
        var eventType = contentTypeService.Get(A.EventType);
        if (eventType is null) return;

        foreach (var evt in contentService.GetPagedOfType(eventType.Id, 0, 1000, out _, null))
        {
            if ((await eventTickets.GetByEventAsync(evt.Id)).Count > 0) continue;

            var cats = await pricing.GetAvailableAsync(evt.Id);
            if (cats.Count == 0) continue;

            var a = cats[0];
            var b = cats.Count > 1 ? cats[1] : cats[0];

            await eventTickets.SaveAsync(EventTicket.Create(evt.Id, a.Category, a.Price, null));
            await eventTickets.SaveAsync(EventTicket.Create(evt.Id, b.Category, b.Price, null));

            var redeemed = EventTicket.Create(evt.Id, a.Category, a.Price, null);
            redeemed.Redeem();
            await eventTickets.SaveAsync(redeemed);

            logger.LogInformation("PricingCatalogSeeder: seeded demo tickets for event '{Name}'.", evt.Name);
        }
    }
}
