namespace RedAnts.Features.Ticketing.Ports;

public sealed record FlexPrintLayout(
    double PageWidthMm,
    double PageHeightMm,
    double QrXMm,
    double QrYMm,
    double QrSizeMm,
    double CodeFontPt,
    bool ShowCode);

public interface IFlexTicketPrinter
{
    Task<byte[]?> BuildAsync(int bundleId, byte[] templatePdf, FlexPrintLayout layout);
}
