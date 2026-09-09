using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.FlexTickets.Infrastructure;

public sealed class FlexBundleTicketsReader(IScopeProvider scopeProvider, ITicketTokens tokens, IPublicBaseUrl publicUrl) : IFlexBundleTicketsReader
{
    private const string TicketSelect =
        "SELECT t.Id, t.Uuid, t.SeasonId, t.Category, t.Status, t.Redeemed, t.RedeemedEventId, t.CreatedAt, t.BoxOffice, v.IsInside AS InsideFlag, " +
        "cb.CreatedByName AS CreatorName, cb.CreatedByEmail AS CreatorEmail, bb.Reference AS BundleReference, " +
        "t.BuyerType, t.BuyerFirstName, t.BuyerLastName, t.BuyerCompany, t.BuyerEmail, " +
        "t.Salutation, t.Birthday, t.Street, t.AddressLine2, t.PostalCode, t.City, t.Country, t.Phone, " +
        "CASE WHEN EXISTS (SELECT 1 FROM EventTickets et WHERE et.OriginType = @1 AND et.OriginCardUuid = t.Uuid) THEN 1 ELSE 0 END AS Converted " +
        "FROM SeasonSingleTickets t " +
        "LEFT JOIN TicketEventVisits v ON v.TicketUuid = t.Uuid AND v.EventId = t.RedeemedEventId " +
        "LEFT JOIN FlexTicketBundles cb ON cb.Id = COALESCE(t.OriginBundleId, CASE WHEN t.BoxOffice = 1 THEN NULL ELSE t.BundleId END) " +
        "LEFT JOIN FlexTicketBundles bb ON bb.Id = t.BundleId ";

    public Task<IReadOnlyList<FlexTicketRow>> GetByBundleAsync(int bundleId) =>
        FetchAsync("WHERE t.BundleId = @0 ORDER BY t.CreatedAt", bundleId);

    public Task<IReadOnlyList<FlexTicketRow>> GetBySeasonAsync(int seasonId) =>
        FetchAsync("WHERE t.SeasonId = @0 ORDER BY t.CreatedAt", seasonId);

    public Task<IReadOnlyList<FlexTicketRow>> GetByBundlesAsync(IReadOnlyCollection<int> bundleIds) =>
        bundleIds.Count == 0
            ? Task.FromResult<IReadOnlyList<FlexTicketRow>>([])
            : FetchAsync("WHERE t.BundleId IN (@0) ORDER BY bb.Reference, t.Id", bundleIds);

    private async Task<IReadOnlyList<FlexTicketRow>> FetchAsync(string where, object scope0)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<FlexTicketRecordRow>(
            TicketSelect + where, new object[] { scope0, (int)TicketType.SeasonSingle });
        return rows.Select(Map).ToList();
    }

    private FlexTicketRow Map(FlexTicketRecordRow r)
    {
        var uuid = Guid.TryParse(r.Uuid, out var parsed) ? parsed : Guid.Empty;
        return new FlexTicketRow(
            uuid, r.SeasonId, (TicketStatus)r.Status, r.Redeemed, r.RedeemedEventId, r.CreatedAt,
            uuid == Guid.Empty ? "" : publicUrl.TicketUrl(tokens.CreateShort(uuid)),
            (TicketCategory)r.Category, r.InsideFlag, r.Converted == 1, r.BoxOffice,
            r.CreatorName, r.CreatorEmail,
            CardHolder.Create((BuyerType)(r.BuyerType ?? 0), r.Salutation, r.BuyerCompany,
                r.BuyerFirstName, r.BuyerLastName, r.Birthday is { } bd ? DateOnly.FromDateTime(bd) : null,
                r.BuyerEmail, r.Street, r.AddressLine2, r.PostalCode, r.City, r.Country, r.Phone),
            r.BundleReference);
    }

    public sealed class FlexTicketRecordRow
    {
        public int Id { get; set; }
        public string Uuid { get; set; } = "";
        public int SeasonId { get; set; }
        public int Category { get; set; }
        public int Status { get; set; }
        public bool Redeemed { get; set; }
        public int? RedeemedEventId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool? InsideFlag { get; set; }
        public int Converted { get; set; }
        public bool BoxOffice { get; set; }
        public string? CreatorName { get; set; }
        public string? CreatorEmail { get; set; }
        public string? BundleReference { get; set; }
        public int? BuyerType { get; set; }
        public string? BuyerFirstName { get; set; }
        public string? BuyerLastName { get; set; }
        public string? BuyerCompany { get; set; }
        public string? BuyerEmail { get; set; }
        public string? Salutation { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Street { get; set; }
        public string? AddressLine2 { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
    }
}
