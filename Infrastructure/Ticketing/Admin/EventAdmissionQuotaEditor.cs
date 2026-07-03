using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

/// <summary>Reads the admission quota straight from the <c>EventPrices</c> table (one query for the whole
/// table) and writes it back through the pricing port so the rest of the price set (category rows, total
/// sales quota) is preserved. Reading directly keeps the table load to a single query; writing goes
/// through <see cref="IEventPrices"/> to stay within the domain's save logic.</summary>
public sealed class EventAdmissionQuotaEditor(IScopeProvider scopeProvider, IEventPrices eventPrices)
    : IEventAdmissionQuota
{
    public async Task<IReadOnlyDictionary<int, int?>> GetAllAsync()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<EventQuotaRow>(
            "SELECT EventId, AdmissionQuota FROM EventPrices");
        var map = new Dictionary<int, int?>();
        foreach (var r in rows) map[r.EventId] = r.AdmissionQuota;
        return map;
    }

    public async Task SetAsync(int eventId, int? admissionQuota)
    {
        var existing = await eventPrices.GetByEventAsync(eventId);
        var updated = existing is null
            ? EventPrice.Create(eventId, null, admissionQuota, [])
            : EventPrice.FromPersistence(existing.Id, existing.EventId, existing.TotalSalesQuota,
                admissionQuota, existing.Categories);
        await eventPrices.SaveAsync(updated);
    }

    /// <summary>Projection for the quota query (mapped by column name).</summary>
    public sealed class EventQuotaRow
    {
        public int EventId { get; set; }
        public int? AdmissionQuota { get; set; }
    }
}

/// <summary>Registers the Anlässe admission-quota editor (auto-discovered via <c>.AddComposers()</c>).</summary>
public sealed class EventAdmissionQuotaEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IEventAdmissionQuota, EventAdmissionQuotaEditor>();
}
