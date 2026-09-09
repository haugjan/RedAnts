using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public sealed record WebTicketPdf(byte[] Bytes, string FileName);

public static class GetWebTicketPdf
{
    public sealed record Query(string Token);

    public sealed class Handler(WebTicketResolution resolution, IQrCodeRenderer qr, IPublicBaseUrl publicUrl, ITicketPdf pdf)
    {
        public async Task<WebTicketPdf?> HandleAsync(Query query)
        {
            var resolved = await resolution.ResolveAsync(query.Token);
            if (resolved is not { Issued.Status: TicketStatus.Valid }) return null;

            var (data, issued) = resolved;
            var context = await resolution.ContextAsync(data.Type, data.ScopeId);
            var ticketRef = WebTicketResolution.TicketRef(data.Uuid);
            var bytes = pdf.Render(new TicketPdfModel(
                Kicker: TicketDisplay.Kicker(data.Type),
                TypeLabel: WebTicketResolution.DisplayTitle(data.Type, issued),
                ScopeName: context.ScopeName,
                DateText: context.DateText,
                CategoryLabel: WebTicketResolution.CategoryLabel(issued),
                HolderName: WebTicketResolution.FirstNonEmpty(issued?.CustomName, issued?.HolderName ?? issued?.BuyerName),
                TicketRef: ticketRef,
                AccentHex: WebTicketResolution.TypeAccentHex(data.Type),
                QrPng: qr.RenderPng(resolution.QrUrl(data.Uuid, publicUrl), 10),
                VenueName: context.VenueName,
                Admissions: issued?.Admissions ?? 1));
            return new WebTicketPdf(bytes, $"redants-ticket-{ticketRef}.pdf");
        }
    }
}
