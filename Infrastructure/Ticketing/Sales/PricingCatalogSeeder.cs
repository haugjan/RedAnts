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

/// <summary>
/// Idempotent boot seed for the pricing catalog: gives every event and season a price set (0..1) with
/// the base categories if it has none, and seeds a few demo event tickets so the admin list has data
/// before checkout exists. Runs every boot; only fills what is missing.
/// </summary>
public sealed class PricingCatalogSeeder(
    IEventPrices eventPrices,
    ISeasonPrices seasonPrices,
    IEventPricing pricing,
    IEventTickets eventTickets,
    IContentService contentService,
    IContentTypeService contentTypeService,
    ILogger<PricingCatalogSeeder> logger) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    // Base categories assigned to a fresh event/season price set (category, sale price). Quotas start
    // unlimited (null); the admin sets the Kontingente. The reduced variants are added per event/season.
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

    // Demo data so the admin ticket list has something to show before checkout exists. Idempotent:
    // seeds a few event tickets (some redeemed) per event without any.
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

            // Two open (not yet redeemed).
            await eventTickets.SaveAsync(EventTicket.Create(evt.Id, a.Category, a.Price, null));
            await eventTickets.SaveAsync(EventTicket.Create(evt.Id, b.Category, b.Price, null));

            // One redeemed (admitted at the event).
            var redeemed = EventTicket.Create(evt.Id, a.Category, a.Price, null);
            redeemed.Redeem();
            await eventTickets.SaveAsync(redeemed);

            logger.LogInformation("PricingCatalogSeeder: seeded demo tickets for event '{Name}'.", evt.Name);
        }
    }
}
