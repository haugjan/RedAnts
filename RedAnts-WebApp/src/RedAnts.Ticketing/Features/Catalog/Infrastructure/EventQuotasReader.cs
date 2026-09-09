using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Catalog.Infrastructure;

public sealed class EventQuotasReader(IScopeProvider scopeProvider) : IEventQuotasReader
{
    public Task<IReadOnlyDictionary<int, int?>> GetAdmissionQuotasAsync() =>
        ReadAsync("SELECT EventId, AdmissionQuota AS Quota FROM EventPrices");

    public Task<IReadOnlyDictionary<int, int?>> GetSalesQuotasAsync() =>
        ReadAsync("SELECT EventId, TotalSalesQuota AS Quota FROM EventPrices");

    private async Task<IReadOnlyDictionary<int, int?>> ReadAsync(string sql)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<EventQuotaRow>(sql);
        var map = new Dictionary<int, int?>();
        foreach (var row in rows) map[row.EventId] = row.Quota;
        return map;
    }

    public sealed class EventQuotaRow
    {
        public int EventId { get; set; }
        public int? Quota { get; set; }
    }
}

public sealed class EventQuotasReaderComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IEventQuotasReader, EventQuotasReader>();
}
