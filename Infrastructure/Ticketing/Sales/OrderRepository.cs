using NPoco;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;
// PaymentMethod exists in both RedAnts.Domain.Ticketing and ...Sales; the sales one is authoritative here.
using PaymentMethod = RedAnts.Domain.Ticketing.Sales.PaymentMethod;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class OrderRepository(IScopeProvider scopeProvider) : IOrders
{
    public async Task<Order> SaveAsync(Order order)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var billing = order.BillingAddress;
        var row = new OrderRecord
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            BillingFirstName = billing.FirstName,
            BillingLastName = billing.LastName,
            BillingStreet = billing.Street,
            BillingAddressLine2 = billing.AddressLine2,
            BillingPostalCode = billing.PostalCode.Value,
            BillingCity = billing.City,
            BillingCountry = billing.Country,
            BillingEmail = billing.Email,
            BillingPhone = billing.Phone,
            Currency = order.Currency,
            SubtotalNet = order.SubtotalNet,
            VatRate = order.VatRate,
            VatAmount = order.VatAmount,
            TotalGross = order.TotalGross,
            SellerUid = order.SellerUid,
            PaymentMethod = (int)order.PaymentMethod,
            Status = (int)order.Status,
            CreatedAt = order.CreatedAt,
            PaidAt = order.PaidAt
        };
        if (row.Id == 0) await scope.Database.InsertAsync(row);
        else await scope.Database.UpdateAsync(row);
        return Map(row);
    }

    /// <summary>Sequential per-year number like <c>2026-000123</c>. Good enough for the guest checkout;
    /// a real system would use a gapless sequence with locking.</summary>
    public async Task<string> NextOrderNumberAsync()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var year = DateTime.UtcNow.Year;
        var prefix = $"{year}-";
        var count = await scope.Database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Orders WHERE OrderNumber LIKE @0", prefix + "%");
        return $"{prefix}{count + 1:000000}";
    }

    private static Order Map(OrderRecord r) =>
        Order.FromPersistence(
            r.Id,
            r.OrderNumber,
            BillingAddress.FromPersistence(
                r.BillingFirstName, r.BillingLastName, r.BillingStreet, r.BillingAddressLine2,
                r.BillingPostalCode, r.BillingCity, r.BillingCountry, r.BillingEmail, r.BillingPhone),
            r.Currency,
            r.SubtotalNet,
            r.VatRate,
            r.VatAmount,
            r.TotalGross,
            r.SellerUid,
            (PaymentMethod)r.PaymentMethod,
            (OrderStatus)r.Status,
            r.CreatedAt,
            r.PaidAt);
}
