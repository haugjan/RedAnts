using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Infrastructure.Ticketing.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

public sealed class TicketPrintSettingsRepository(IScopeProvider scopeProvider) : ITicketPrintSettings
{
    public async Task<TicketPrintLayout> GetAsync(TicketType type)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var record = await scope.Database.FirstOrDefaultAsync<TicketPrintSettingsRecord>(
            "WHERE TicketType = @0", (int)type);
        return record is null ? TicketPrintLayout.Default : Map(record);
    }

    public async Task SaveAsync(TicketType type, TicketPrintLayout layout)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var record = await db.FirstOrDefaultAsync<TicketPrintSettingsRecord>(
            "WHERE TicketType = @0", (int)type);
        if (record is null)
        {
            await db.InsertAsync(ToRecord(new TicketPrintSettingsRecord { TicketType = (int)type }, layout));
        }
        else
        {
            await db.UpdateAsync(ToRecord(record, layout));
        }
    }

    private static TicketPrintLayout Map(TicketPrintSettingsRecord r) => new(
        (double)r.PageWMm, (double)r.PageHMm, (double)r.QrXMm, (double)r.QrYMm, (double)r.QrSizeMm,
        (double)r.CodeFontPt, r.ShowName,
        (double)(r.NameXMm ?? (decimal)TicketPrintLayout.Default.NameXMm),
        (double)(r.NameYMm ?? (decimal)TicketPrintLayout.Default.NameYMm),
        (double)(r.NameFontPt ?? (decimal)TicketPrintLayout.Default.NameFontPt),
        (double)(r.NameMaxWidthMm ?? (decimal)TicketPrintLayout.Default.NameMaxWidthMm));

    private static TicketPrintSettingsRecord ToRecord(TicketPrintSettingsRecord r, TicketPrintLayout l)
    {
        r.PageWMm = (decimal)l.PageWidthMm;
        r.PageHMm = (decimal)l.PageHeightMm;
        r.QrXMm = (decimal)l.QrXMm;
        r.QrYMm = (decimal)l.QrYMm;
        r.QrSizeMm = (decimal)l.QrSizeMm;
        r.CodeFontPt = (decimal)l.CodeFontPt;
        r.ShowName = l.ShowName;
        r.NameXMm = (decimal)l.NameXMm;
        r.NameYMm = (decimal)l.NameYMm;
        r.NameFontPt = (decimal)l.NameFontPt;
        r.NameMaxWidthMm = (decimal)l.NameMaxWidthMm;
        return r;
    }
}
