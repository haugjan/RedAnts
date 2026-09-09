using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.EventBundles.Infrastructure;

public sealed class EventBundleTicketsReader(IScopeProvider scopeProvider, ITicketTokens tokens, IPublicBaseUrl publicUrl) : IEventBundleTicketsReader
{
    public Task<IReadOnlyList<EventBundleTicketRow>> GetByBundleAsync(int bundleId) => GetByBundlesAsync([bundleId]);

    public async Task<IReadOnlyList<EventBundleTicketRow>> GetByBundlesAsync(IReadOnlyCollection<int> bundleIds)
    {
        if (bundleIds.Count == 0) return [];
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<Row>(
            "SELECT t.Uuid, t.EventId, b.Reference, t.Category, " +
            "t.BuyerType, t.BuyerFirstName, t.BuyerLastName, t.BuyerCompany, t.Email AS BuyerEmail, " +
            "t.Salutation, t.Birthday, t.Street, t.AddressLine2, t.PostalCode, t.City, t.Country, t.Phone " +
            "FROM EventTickets t " +
            "JOIN EventTicketBundles b ON b.Id = t.BundleId " +
            "WHERE t.BundleId IN (@0) ORDER BY b.Reference, t.Id",
            new object[] { bundleIds });
        return rows
            .Select(r => (Uuid: Guid.TryParse(r.Uuid, out var g) ? g : Guid.Empty, Row: r))
            .Where(x => x.Uuid != Guid.Empty)
            .Select(x => new EventBundleTicketRow(
                x.Uuid, x.Row.EventId, x.Row.Reference ?? "", publicUrl.TicketUrl(tokens.CreateShort(x.Uuid)),
                (TicketCategory)x.Row.Category,
                CardHolder.Create((BuyerType)(x.Row.BuyerType ?? 0), x.Row.Salutation, x.Row.BuyerCompany,
                    x.Row.BuyerFirstName, x.Row.BuyerLastName, x.Row.Birthday is { } bd ? DateOnly.FromDateTime(bd) : null,
                    x.Row.BuyerEmail, x.Row.Street, x.Row.AddressLine2, x.Row.PostalCode, x.Row.City, x.Row.Country, x.Row.Phone)))
            .ToList();
    }

    public sealed class Row
    {
        public string Uuid { get; set; } = "";
        public int EventId { get; set; }
        public string Reference { get; set; } = "";
        public int Category { get; set; }
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
