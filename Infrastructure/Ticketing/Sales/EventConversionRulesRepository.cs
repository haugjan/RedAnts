using Microsoft.Extensions.DependencyInjection;
using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class EventConversionRulesRepository(IScopeProvider scopeProvider, IEventPrices eventPrices) : IEventConversionRules
{
    public async Task<bool> GetConversionOnlyAsync(int eventId) =>
        (await eventPrices.GetByEventAsync(eventId))?.ConversionOnly ?? false;

    public async Task SetConversionOnlyAsync(int eventId, bool value)
    {
        var existing = await eventPrices.GetByEventAsync(eventId);
        var updated = existing is null
            ? EventPrice.Create(eventId, null, null, [], conversionOnly: value)
            : EventPrice.FromPersistence(existing.Id, existing.EventId, existing.TotalSalesQuota,
                existing.AdmissionQuota, existing.Categories, conversionOnly: value);
        await eventPrices.SaveAsync(updated);
    }

    public async Task<IReadOnlyList<EventConversionRule>> GetAllAsync()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<EventConversionRuleRecord>("");
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<EventConversionRule>> GetByEventAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<EventConversionRuleRecord>("WHERE EventId = @0", eventId);
        return rows.Select(Map).ToList();
    }

    public async Task SetAsync(int eventId, TicketType cardType, decimal? discount)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        await db.ExecuteAsync("DELETE FROM EventConversionRules WHERE EventId = @0 AND CardType = @1",
            eventId, (int)cardType);
        if (discount is { } d)
            await db.InsertAsync(new EventConversionRuleRecord
            {
                EventId = eventId,
                CardType = (int)cardType,
                Discount = Math.Max(0m, decimal.Round(d, 2))
            });
    }

    private static EventConversionRule Map(EventConversionRuleRecord r) =>
        new(r.EventId, (TicketType)r.CardType, r.Discount);
}

public sealed class EventConversionRulesComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IEventConversionRules, EventConversionRulesRepository>();
}
