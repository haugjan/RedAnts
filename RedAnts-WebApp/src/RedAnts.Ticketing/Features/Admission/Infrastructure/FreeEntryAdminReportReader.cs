using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admission.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Admission.Infrastructure;

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
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsInside { get; set; }
        public string? GrantedBy { get; set; }
        public int? Category { get; set; }
    }
}

public sealed class FreeEntryAdminReportComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<IFreeEntryAdminReport, FreeEntryAdminReportReader>();
    }
}
