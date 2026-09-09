using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Catalog.Infrastructure;

public sealed class EventConversionRulesRepository(IScopeProvider scopeProvider, IEventPriceRepository eventPrices) : IEventConversionRuleRepository
{
    public async Task<bool> GetConversionOnlyAsync(int eventId) =>
        (await eventPrices.GetByEventAsync(eventId))?.ConversionOnly ?? false;

    public async Task SetConversionOnlyAsync(int eventId, bool value)
    {
        var existing = await eventPrices.GetByEventAsync(eventId);
        var updated = existing?.WithConversionOnly(value)
            ?? EventPrice.Create(eventId, null, null, [], conversionOnly: value);
        await eventPrices.SaveAsync(updated);
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
}

public sealed class EventConversionRuleReader(IScopeProvider scopeProvider, IEventPriceRepository eventPrices) : IEventConversionRuleReader
{
    public async Task<IReadOnlyList<EventConversionRule>> GetByEventAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<EventConversionRuleRecord>("WHERE EventId = @0", eventId);
        return rows.Select(r => new EventConversionRule(r.EventId, (TicketType)r.CardType, r.Discount)).ToList();
    }

    public async Task<bool> GetConversionOnlyAsync(int eventId) =>
        (await eventPrices.GetByEventAsync(eventId))?.ConversionOnly ?? false;
}

public sealed class EventConversionRulesComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<IEventConversionRuleRepository, EventConversionRulesRepository>();
        builder.Services.AddScoped<IEventConversionRuleReader, EventConversionRuleReader>();
    }
}
