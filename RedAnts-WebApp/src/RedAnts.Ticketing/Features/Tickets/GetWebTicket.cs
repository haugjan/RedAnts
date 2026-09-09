using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public sealed record WebTicket(
    Guid Uuid,
    bool Found,
    bool Valid,
    string Kicker,
    string TypeLabel,
    string ScopeName,
    string? DateText,
    string? CategoryLabel,
    string? HolderName,
    string TicketRef,
    string QrSvg,
    byte[] QrPng,
    string? HomeLogo,
    string? AwayLogo,
    string TypeKey,
    string? VenueName,
    int Admissions,
    string? CustomName,
    string? HolderDefault,
    IReadOnlyList<UpcomingMatch> Upcoming);

public static class GetWebTicket
{
    public sealed record Query(string Token);

    public sealed class Handler(WebTicketResolution resolution, IQrCodeRenderer qr, IPublicBaseUrl publicUrl)
    {
        public async Task<WebTicket?> HandleAsync(Query query)
        {
            var resolved = await resolution.ResolveAsync(query.Token);
            if (resolved is null) return null;

            var (data, issued) = resolved;
            var context = await resolution.ContextAsync(data.Type, data.ScopeId);
            var qrUrl = resolution.QrUrl(data.Uuid, publicUrl);
            var holderDefault = issued?.HolderName ?? issued?.BuyerName;

            return new WebTicket(
                Uuid: data.Uuid,
                Found: issued is not null,
                Valid: issued is { Status: TicketStatus.Valid },
                Kicker: TicketDisplay.Kicker(data.Type),
                TypeLabel: WebTicketResolution.DisplayTitle(data.Type, issued),
                ScopeName: context.ScopeName,
                DateText: data.Type == TicketType.EventTicket ? context.DateText : null,
                CategoryLabel: WebTicketResolution.CategoryLabel(issued),
                HolderName: WebTicketResolution.FirstNonEmpty(issued?.CustomName, holderDefault),
                TicketRef: WebTicketResolution.TicketRef(data.Uuid),
                QrSvg: qr.RenderSvg(qrUrl),
                QrPng: qr.RenderPng(qrUrl, 8),
                HomeLogo: context.HomeLogo,
                AwayLogo: context.AwayLogo,
                TypeKey: WebTicketResolution.TypeKey(data.Type, issued?.MemberCategory),
                VenueName: context.VenueName,
                Admissions: issued?.Admissions ?? 1,
                CustomName: issued?.CustomName,
                HolderDefault: holderDefault,
                Upcoming: await resolution.UpcomingAsync(20));
        }
    }
}
