using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admission.Admin;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Tickets.Infrastructure;

public sealed class EventTicketListReader(IScopeProvider scopeProvider, ITicketTokens tokens, IPublicBaseUrl publicUrl)
    : IEventTicketListReader
{
    public async Task<IReadOnlyList<EventTicketRow>> GetByEventAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<Row>(@"
            SELECT t.Uuid, t.EventId, t.Category, t.Price, t.OrderId, t.Status, t.CreatedAt, t.Redeemed,
                   t.BuyerType, t.BuyerFirstName, t.BuyerLastName, t.BuyerCompany, t.CreatedByName, t.CreatedByEmail,
                   t.BundleId, t.OriginType, t.OriginCardUuid, t.Salutation, t.Birthday, t.Email, t.Street, t.AddressLine2,
                   t.PostalCode, t.City, t.Country, t.Phone,
                   b.Reference AS BundleReference, v.IsInside
            FROM EventTickets t
            LEFT JOIN EventTicketBundles b ON b.Id = t.BundleId
            LEFT JOIN TicketEventVisits v ON v.EventId = t.EventId AND v.TicketUuid = t.Uuid
            WHERE t.EventId = @0
            ORDER BY t.CreatedAt, t.Id", eventId);
        return rows.Select(Map).Where(r => r.Uuid != Guid.Empty).ToList();
    }

    public async Task<IReadOnlyList<EventTicketBundleRow>> GetBundlesByEventAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<BundleRow>(@"
            SELECT b.Id, b.Reference, b.Category, COUNT(t.Id) AS TicketCount
            FROM EventTicketBundles b
            LEFT JOIN EventTickets t ON t.BundleId = b.Id
            WHERE b.EventId = @0
            GROUP BY b.Id, b.Reference, b.Category, b.CreatedAt
            ORDER BY b.CreatedAt DESC", eventId);
        return rows.Select(b => new EventTicketBundleRow(b.Id, b.Reference, (TicketCategory)b.Category, b.TicketCount)).ToList();
    }

    private EventTicketRow Map(Row r)
    {
        var uuid = Guid.TryParse(r.Uuid, out var parsed) ? parsed : Guid.Empty;
        var buyerType = (BuyerType)(r.BuyerType ?? 0);
        var holder = CardHolder.Create(buyerType, r.Salutation, r.BuyerCompany, r.BuyerFirstName, r.BuyerLastName,
            r.Birthday is { } bd ? DateOnly.FromDateTime(bd) : null,
            r.Email, r.Street, r.AddressLine2, r.PostalCode, r.City, r.Country, r.Phone);
        return new EventTicketRow(
            uuid,
            publicUrl.TicketUrl(tokens.CreateShort(uuid)),
            r.EventId,
            (TicketCategory)r.Category,
            r.Price,
            (TicketStatus)r.Status,
            r.Redeemed,
            r.IsInside,
            RedemptionStateExtensions.Derive(r.Redeemed, r.IsInside),
            r.CreatedAt,
            r.CreatedByName,
            r.CreatedByEmail,
            r.OrderId,
            r.BundleId,
            r.BundleReference,
            r.OriginType is { } ot ? (TicketType)ot : null,
            Guid.TryParse(r.OriginCardUuid, out var origin) ? origin : (Guid?)null,
            Buyer.FromPersistence(r.BuyerType ?? 0, r.BuyerFirstName, r.BuyerLastName, r.BuyerCompany)?.DisplayName,
            holder);
    }

    public sealed class Row
    {
        public string Uuid { get; set; } = "";
        public int EventId { get; set; }
        public int Category { get; set; }
        public decimal Price { get; set; }
        public int? OrderId { get; set; }
        public int Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool Redeemed { get; set; }
        public int? BuyerType { get; set; }
        public string? BuyerFirstName { get; set; }
        public string? BuyerLastName { get; set; }
        public string? BuyerCompany { get; set; }
        public string? CreatedByName { get; set; }
        public string? CreatedByEmail { get; set; }
        public int? BundleId { get; set; }
        public int? OriginType { get; set; }
        public string? OriginCardUuid { get; set; }
        public string? Salutation { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Email { get; set; }
        public string? Street { get; set; }
        public string? AddressLine2 { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
        public string? BundleReference { get; set; }
        public bool? IsInside { get; set; }
    }

    public sealed class BundleRow
    {
        public int Id { get; set; }
        public string Reference { get; set; } = "";
        public int Category { get; set; }
        public int TicketCount { get; set; }
    }
}
