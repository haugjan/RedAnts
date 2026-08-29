using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

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

    public sealed class EventQuotaRow
    {
        public int EventId { get; set; }
        public int? AdmissionQuota { get; set; }
    }
}

public sealed class EventAdmissionQuotaEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IEventAdmissionQuota, EventAdmissionQuotaEditor>();
}
