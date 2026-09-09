using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admin;
using RedAnts.Ticketing.Features.Admission;
using RedAnts.Ticketing.Features.Catalog.Pricing;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Catalog.Infrastructure;

public sealed class EventsForAdminReader(
    IEventReader events,
    IEventQuotasReader quotas,
    IEventLinkReader links,
    IEventAdmissionReader admission,
    IContentCreateLinks createLinks,
    IScopeProvider scopeProvider) : IEventsForAdminReader
{
    public async Task<EventsForAdmin> GetBySeasonAsync(int seasonId)
    {
        var seasonEvents = await events.GetBySeasonAsync(seasonId);
        var counts = await admission.GetCountsByEventAsync();
        var admissionQuotas = await quotas.GetAdmissionQuotasAsync();
        var salesQuotas = await quotas.GetSalesQuotasAsync();
        var eventLinks = await links.GetBySeasonAsync(seasonId);
        var conversion = await ConversionAsync(seasonEvents.Select(e => e.Id).ToList());

        var rows = seasonEvents.Select(e =>
        {
            var rules = conversion.Rules.GetValueOrDefault(e.Id) ?? [];
            var flex = rules.FirstOrDefault(r => r.CardType == (int)TicketType.SeasonSingle);
            var eventLink = eventLinks.GetValueOrDefault(e.Id);
            return new EventForAdmin(e.Id, e.Name, e.Date, e.StartTime, e.TimeUnknown, e.Status,
                admissionQuotas.GetValueOrDefault(e.Id), salesQuotas.GetValueOrDefault(e.Id),
                counts.GetValueOrDefault(e.Id) ?? EventAdmissionCounts.Empty,
                eventLink?.Public, eventLink?.Intern,
                rules.Any(r => r.CardType == (int)TicketType.SeasonPass),
                rules.Any(r => r.CardType == (int)TicketType.MemberCard),
                flex is null ? null : decimal.Round(flex.Discount, 2),
                conversion.ConversionOnly.Contains(e.Id));
        }).ToList();
        return new EventsForAdmin(rows, await createLinks.CreateEventUrlAsync(seasonId));
    }

    public async Task<IReadOnlyList<EventTierPrice>> GetTierPricesAsync(int eventId)
    {
        var evt = await events.FindByIdAsync(eventId);
        if (evt is null) return [];

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var tiers = await db.FetchAsync<SeasonPriceTierRecord>(
            "WHERE SeasonId = @0 AND PromoOfTierId IS NULL ORDER BY SortOrder, Id", evt.SeasonId);
        var parent = await db.FirstOrDefaultAsync<EventPriceRecord>("WHERE EventId = @0", eventId);
        var categories = parent is null
            ? []
            : await db.FetchAsync<EventPriceCategoryRecord>("WHERE EventPriceId = @0", parent.Id);
        var byTier = new Dictionary<int, EventPriceCategoryRecord>();
        foreach (var c in categories.Where(c => c.TierId is not null))
            byTier[c.TierId!.Value] = c;

        return tiers.Select(t => byTier.TryGetValue(t.Id, out var existing)
                ? new EventTierPrice(t.Id, t.Name, true, decimal.Round(existing.SalePrice, 2), existing.Quota, ToDateOnly(existing.AvailableUntil))
                : new EventTierPrice(t.Id, t.Name, false, 0m, null, null))
            .ToList();
    }

    private async Task<ConversionSettings> ConversionAsync(IReadOnlyList<int> eventIds)
    {
        if (eventIds.Count == 0) return new ConversionSettings(new Dictionary<int, List<EventConversionRuleRecord>>(), []);

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var rules = await db.FetchAsync<EventConversionRuleRecord>("WHERE EventId IN (@0)", eventIds);
        var prices = await db.FetchAsync<EventPriceRecord>("WHERE EventId IN (@0)", eventIds);
        return new ConversionSettings(
            rules.GroupBy(r => r.EventId).ToDictionary(g => g.Key, g => g.ToList()),
            prices.Where(p => p.ConversionOnly).Select(p => p.EventId).ToHashSet());
    }

    private static DateOnly? ToDateOnly(DateTime? value) => value is { } v ? DateOnly.FromDateTime(v) : null;

    private sealed record ConversionSettings(Dictionary<int, List<EventConversionRuleRecord>> Rules, HashSet<int> ConversionOnly);
}
