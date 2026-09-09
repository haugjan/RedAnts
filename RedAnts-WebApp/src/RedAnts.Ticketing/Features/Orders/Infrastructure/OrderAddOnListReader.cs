using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Orders.Infrastructure;

public sealed class OrderAddOnListReader(IScopeProvider scopeProvider) : IOrderAddOnListReader
{
    public async Task<IReadOnlyList<OrderAddOnRow>> GetBySeasonAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var rows = await scope.Database.FetchAsync<AddOnRow>(@"
            SELECT a.Id, a.OrderId, a.Label, a.CategoryName, a.Quantity, a.Price, a.Delivered,
                   o.OrderNumber, o.CreatedAt, o.Status,
                   o.BillingType, o.BillingFirstName, o.BillingLastName, o.BillingCompany, o.BillingEmail
            FROM OrderAddOns a
            JOIN Orders o ON o.Id = a.OrderId
            WHERE a.SeasonId = @0
            ORDER BY a.Delivered, o.CreatedAt DESC", seasonId);

        return rows.Select(r => new OrderAddOnRow(
            r.Id,
            r.OrderId,
            r.OrderNumber,
            r.CreatedAt,
            (OrderStatus)r.Status,
            BuyerName(r),
            r.BillingEmail ?? "",
            r.Label,
            r.CategoryName,
            r.Quantity,
            r.Price,
            r.Delivered)).ToList();
    }

    private static string BuyerName(AddOnRow r)
    {
        if ((BuyerType)(r.BillingType ?? 0) == BuyerType.Company && !string.IsNullOrWhiteSpace(r.BillingCompany))
            return r.BillingCompany!;
        var name = $"{r.BillingFirstName} {r.BillingLastName}".Trim();
        return string.IsNullOrEmpty(name) ? "—" : name;
    }

    public sealed class AddOnRow
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Label { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public bool Delivered { get; set; }
        public string OrderNumber { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; }
        public int Status { get; set; }
        public int? BillingType { get; set; }
        public string? BillingFirstName { get; set; }
        public string? BillingLastName { get; set; }
        public string? BillingCompany { get; set; }
        public string? BillingEmail { get; set; }
    }
}
