namespace RedAnts.Features.Ticketing.Tickets;

public sealed record TicketPdfModel(
    string TypeLabel,
    string ScopeName,
    string? DateText,
    string? CategoryLabel,
    string? HolderName,
    string TicketRef,
    string AccentHex,
    byte[] QrPng,
    string Kicker = "",
    string? VenueName = null,
    int Admissions = 1);

public interface ITicketPdf
{
    byte[] Render(TicketPdfModel model);
}
