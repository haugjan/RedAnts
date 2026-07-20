using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class EventSalesQuotaEditor(IScopeProvider scopeProvider, IEventPrices eventPrices)
    : IEventSalesQuota
{
    public async Task<IReadOnlyDictionary<int, int?>> GetAllAsync()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<EventSalesQuotaRow>(
            "SELECT EventId, TotalSalesQuota FROM EventPrices");
        var map = new Dictionary<int, int?>();
        foreach (var r in rows) map[r.EventId] = r.TotalSalesQuota;
        return map;
    }

    public async Task SetAsync(int eventId, int? salesQuota)
    {
        var existing = await eventPrices.GetByEventAsync(eventId);
        var updated = existing is null
            ? EventPrice.Create(eventId, salesQuota, null, [])
            : EventPrice.FromPersistence(existing.Id, existing.EventId, salesQuota,
                existing.AdmissionQuota, existing.Categories);
        await eventPrices.SaveAsync(updated);
    }

    public sealed class EventSalesQuotaRow
    {
        public int EventId { get; set; }
        public int? TotalSalesQuota { get; set; }
    }
}

public sealed class EventSalesQuotaEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IEventSalesQuota, EventSalesQuotaEditor>();
}
