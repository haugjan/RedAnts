using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public sealed record TicketPrintLayout(
    double PageWidthMm,
    double PageHeightMm,
    double QrXMm,
    double QrYMm,
    double QrSizeMm,
    double CodeFontPt,
    bool ShowName,
    double NameXMm,
    double NameYMm,
    double NameFontPt,
    double NameMaxWidthMm)
{
    public static TicketPrintLayout Default { get; } =
        new(91, 61, 5, 5, 25, 8, false, 34, 8, 9, 52);
}

public sealed record TicketPrintItem(Guid Uuid, string? HolderName);

public interface ITicketPrinter
{
    Task<byte[]> BuildAsync(IReadOnlyList<TicketPrintItem> items, byte[] templatePdf, TicketPrintLayout layout);
}

public interface ITicketPrintSettings
{
    Task<TicketPrintLayout> GetAsync(TicketType type);
    Task SaveAsync(TicketType type, TicketPrintLayout layout);
}
