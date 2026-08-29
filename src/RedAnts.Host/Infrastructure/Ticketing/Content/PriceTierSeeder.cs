using NPoco;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Infrastructure.Ticketing.Sales;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Content;

public sealed class PriceTierSeederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, PriceTierSeeder>();
}

public sealed class PriceTierSeeder(IScopeProvider scopeProvider, ISeasons seasons, IEvents events)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        var allSeasons = await seasons.GetAllAsync();
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        foreach (var s in allSeasons)
        {
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM SeasonPriceTiers WHERE SeasonId = @0", s.Id);
            if (count == 0) await SeedDefaultsAsync(db, s.Id);
        }

        var map = (await db.FetchAsync<SeasonPriceTierRecord>("WHERE LegacyCategory IS NOT NULL"))
            .GroupBy(t => t.SeasonId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(t => t.LegacyCategory!.Value, t => t.Id));

        var eventToSeason = (await events.GetAllAsync())
            .GroupBy(e => e.Id).ToDictionary(g => g.Key, g => g.First().SeasonId);
        var seasonPriceToSeason = (await db.FetchAsync<IdRefRow>("SELECT Id, SeasonId AS Ref FROM SeasonPrices"))
            .ToDictionary(r => r.Id, r => r.Ref);
        var eventPriceToEvent = (await db.FetchAsync<IdRefRow>("SELECT Id, EventId AS Ref FROM EventPrices"))
            .ToDictionary(r => r.Id, r => r.Ref);

        await BackfillColumnAsync(db, "SeasonPasses", "SeasonId", map, sid => sid);
        await BackfillColumnAsync(db, "SeasonSingleTickets", "SeasonId", map, sid => sid);
        await BackfillColumnAsync(db, "OrderAddOns", "SeasonId", map, sid => sid);
        await BackfillColumnAsync(db, "EventTickets", "EventId", map,
            evId => eventToSeason.GetValueOrDefault(evId));
        await BackfillColumnAsync(db, "SeasonPriceCategories", "SeasonPriceId", map,
            spId => seasonPriceToSeason.GetValueOrDefault(spId));
        await BackfillColumnAsync(db, "EventPriceCategories", "EventPriceId", map,
            epId => eventPriceToEvent.TryGetValue(epId, out var evId) ? eventToSeason.GetValueOrDefault(evId) : 0);
    }

    private static async Task SeedDefaultsAsync(IDatabase db, int seasonId)
    {
        var adult = await InsertAsync(db, seasonId, "Erwachsen", null, null, 0, 0);
        var youth = await InsertAsync(db, seasonId, "Jugend", 19, null, 1, 2);
        await InsertAsync(db, seasonId, "Kind", 5, null, 2, 4);
        await InsertAsync(db, seasonId, "Sonderaktion Erwachsen", null, adult, 0, 1);
        await InsertAsync(db, seasonId, "Sonderaktion Jugend", 19, youth, 1, 3);
    }

    private static async Task<int> InsertAsync(IDatabase db, int seasonId, string name, int? maxAge,
        int? promoOfTierId, int sortOrder, int legacyCategory)
    {
        var rec = new SeasonPriceTierRecord
        {
            SeasonId = seasonId,
            Name = name,
            MaxAge = maxAge,
            PromoOfTierId = promoOfTierId,
            SortOrder = sortOrder,
            LegacyCategory = legacyCategory
        };
        await db.InsertAsync(rec);
        return rec.Id;
    }

    private static async Task BackfillColumnAsync(IDatabase db, string table, string refColumn,
        IReadOnlyDictionary<int, Dictionary<int, int>> map, Func<int, int> resolveSeason)
    {
        var rows = await db.FetchAsync<JoinedRow>(
            $"SELECT Id, {refColumn} AS Ref, Category AS Category FROM {table} WHERE TierId IS NULL");
        foreach (var r in rows)
        {
            var seasonId = resolveSeason(r.Ref);
            if (seasonId > 0 && map.TryGetValue(seasonId, out var m) && m.TryGetValue(r.Category, out var tierId))
                await db.ExecuteAsync($"UPDATE {table} SET TierId = @0 WHERE Id = @1", tierId, r.Id);
        }
    }

    private sealed class IdRefRow { public int Id { get; set; } public int Ref { get; set; } }
    private sealed class JoinedRow { public int Id { get; set; } public int Ref { get; set; } public int Category { get; set; } }
}
