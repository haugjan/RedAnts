using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admin;

namespace RedAnts.Ticketing.Features.EventBundles;

public sealed record EventBundleTicketRow(Guid Uuid, int EventId, string Reference, string TicketUrl,
    TicketCategory Category = TicketCategory.Adult, CardHolder? Holder = null)
{
    public string CardNo => AdminFormat.TicketNo(Uuid);
    public string CategoryLabel => Category.DisplayName();
}
