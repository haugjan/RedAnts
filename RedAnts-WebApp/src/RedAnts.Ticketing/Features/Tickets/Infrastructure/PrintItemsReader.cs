using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.EventBundles;
using RedAnts.Ticketing.Features.FlexTickets;
using RedAnts.Ticketing.Features.MemberCards;
using RedAnts.Ticketing.Features.SeasonPasses;

namespace RedAnts.Ticketing.Features.Tickets.Infrastructure;

public sealed class PrintItemsReader(
    IEventBundleTickets eventBundles,
    IFlexBundleTickets flexBundles,
    ISeasonPassAdminReport seasonReport,
    IMemberCardAdminReport memberReport,
    IIssuedTicketReader issued) : IPrintItemsReader
{
    public async Task<IReadOnlyList<TicketPrintItem>> ResolveAsync(TicketType type, int? bundleId, int? seasonId, string? reference, Guid? uuid)
    {
        if (uuid is { } single)
        {
            var ticket = await issued.FindAsync(single);
            return ticket is null ? [] : [new TicketPrintItem(single, ticket.HolderName ?? ticket.BuyerName)];
        }

        var reff = (reference ?? "").Trim();
        return type switch
        {
            TicketType.EventTicket when bundleId is { } bid =>
                (await eventBundles.GetByBundleAsync(bid))
                    .Select(t => new TicketPrintItem(t.Uuid, t.Holder?.DisplayName)).ToList(),
            TicketType.SeasonSingle when bundleId is { } bid =>
                (await flexBundles.GetByBundlesAsync([bid]))
                    .Select(t => new TicketPrintItem(t.Uuid, t.Holder?.DisplayName)).ToList(),
            TicketType.SeasonPass when seasonId is { } sid =>
                (await seasonReport.GetBySeasonAsync(sid))
                    .Where(p => reff.Length == 0 || string.Equals(p.Reference ?? "", reff, StringComparison.Ordinal))
                    .Select(p => new TicketPrintItem(p.Uuid, p.Holder?.DisplayName ?? p.BuyerName)).ToList(),
            TicketType.MemberCard when seasonId is { } sid =>
                (await memberReport.GetBySeasonAsync(sid))
                    .Where(c => reff.Length == 0 || string.Equals(c.Reference ?? "", reff, StringComparison.Ordinal))
                    .Select(c => new TicketPrintItem(c.Uuid, c.HolderName)).ToList(),
            _ => []
        };
    }
}
