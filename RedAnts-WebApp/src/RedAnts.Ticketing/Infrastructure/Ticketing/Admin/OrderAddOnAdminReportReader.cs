using Microsoft.Extensions.DependencyInjection;
using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class OrderAddOnAdminReportReader(IScopeProvider scopeProvider) : IOrderAddOnAdminReport
{
    public async Task<IReadOnlyList<AddOnDeliveryItem>> GetBySeasonAsync(int seasonId)
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

        return rows.Select(r => new AddOnDeliveryItem
        {
            Id = r.Id,
            OrderId = r.OrderId,
            OrderNumber = r.OrderNumber,
            CreatedAt = r.CreatedAt,
            OrderStatus = (OrderStatus)r.Status,
            BuyerName = BuyerName(r),
            Email = r.BillingEmail ?? "",
            Label = r.Label,
            CategoryName = r.CategoryName,
            Quantity = r.Quantity,
            Price = r.Price,
            Delivered = r.Delivered
        }).ToList();
    }

    public async Task SetDeliveredAsync(int orderAddOnId, bool delivered)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE OrderAddOns SET Delivered = @0 WHERE Id = @1", delivered, orderAddOnId);
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
        public DateTime CreatedAt { get; set; }
        public int Status { get; set; }
        public int? BillingType { get; set; }
        public string? BillingFirstName { get; set; }
        public string? BillingLastName { get; set; }
        public string? BillingCompany { get; set; }
        public string? BillingEmail { get; set; }
    }
}

public sealed class OrderAddOnAdminReportComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IOrderAddOnAdminReport, OrderAddOnAdminReportReader>();
}
