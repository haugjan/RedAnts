using Microsoft.Extensions.DependencyInjection;
using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Infrastructure.Ticketing.Sales;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class FreeEntryAdminReportReader(IScopeProvider scopeProvider) : IFreeEntryAdminReport
{
    public async Task<IReadOnlyList<FreeEntryListItem>> GetByEventAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<FreeEntryRow>(
            "SELECT v.Uuid AS Uuid, v.CreatedAt AS CreatedAt, v.IsInside AS IsInside, f.FreeEntryType AS Category, " +
            "(SELECT MAX(l.ScannedBy) FROM TicketEventVisitsLogs l WHERE l.VisitId = v.Id AND l.Type = @0) AS GrantedBy " +
            "FROM TicketEventVisits v " +
            "LEFT JOIN TicketEventFreeEntries f ON f.VisitId = v.Id " +
            "WHERE v.EventId = @1 AND v.TicketType = @2 " +
            "ORDER BY v.CreatedAt DESC",
            (int)VisitLogType.CheckIn, eventId, (int)TicketType.FreeEntry);

        return rows.Select(r => new FreeEntryListItem(
            Guid.TryParse(r.Uuid, out var uuid) ? uuid : Guid.Empty,
            r.CreatedAt, r.GrantedBy, r.IsInside,
            r.Category is { } c ? (FreeEntryType)c : null)).ToList();
    }

    private sealed class FreeEntryRow
    {
        public string? Uuid { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsInside { get; set; }
        public string? GrantedBy { get; set; }
        public int? Category { get; set; }
    }
}

public sealed class SuFreeEntryQuotaStore(IScopeProvider scopeProvider) : ISuFreeEntryQuota
{
    public async Task<int?> GetAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return await scope.Database.ExecuteScalarAsync<int?>(
            "SELECT SuQuota FROM TicketEventFreeEntryQuotas WHERE EventId = @0", eventId);
    }

    public async Task SetAsync(int eventId, int? quota)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var existing = await db.FirstOrDefaultAsync<EventFreeEntryQuotaRecord>("WHERE EventId = @0", eventId);
        if (existing is null)
            await db.InsertAsync(new EventFreeEntryQuotaRecord { EventId = eventId, SuQuota = quota });
        else
        {
            existing.SuQuota = quota;
            await db.UpdateAsync(existing);
        }
    }
}

public sealed class FreeEntryAdminReportComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<IFreeEntryAdminReport, FreeEntryAdminReportReader>();
        builder.Services.AddScoped<ISuFreeEntryQuota, SuFreeEntryQuotaStore>();
    }
}
