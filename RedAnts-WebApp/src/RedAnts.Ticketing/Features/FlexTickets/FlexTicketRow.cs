using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admin;

namespace RedAnts.Ticketing.Features.FlexTickets;

public sealed record FlexTicketRow(
    Guid Uuid,
    int SeasonId,
    TicketStatus Status,
    bool Redeemed,
    int? RedeemedEventId,
    DateTimeOffset CreatedAt,
    string TicketUrl,
    TicketCategory Category = TicketCategory.Adult,
    bool? IsInside = null,
    bool ConvertedForPurchase = false,
    bool BoxOffice = false,
    string? CreatorName = null,
    string? CreatorEmail = null,
    CardHolder? Holder = null,
    string? BundleReference = null)
{
    public string CardNo => AdminFormat.TicketNo(Uuid);
    public string CategoryLabel => Category.DisplayName();
    public string StatusLabel => Status.DisplayName();
    public bool HasEmail => !string.IsNullOrWhiteSpace(Holder?.Email?.Value);
}
