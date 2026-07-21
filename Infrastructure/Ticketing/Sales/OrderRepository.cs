using NPoco;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;
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
            BillingType = (int)billing.Type,
            BillingFirstName = billing.FirstName,
            BillingLastName = billing.LastName,
            BillingCompany = billing.Company,
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
            PaymentSource = order.PaymentSource is { } ps ? (int)ps : null,
            Status = (int)order.Status,
            CreatedAt = order.CreatedAt,
            PaidAt = order.PaidAt,
            PayrexxGatewayId = order.PayrexxGatewayId,
            FulfillmentPayload = order.FulfillmentPayload
        };
        if (row.Id == 0) await scope.Database.InsertAsync(row);
        else await scope.Database.UpdateAsync(row);
        return Map(row);
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var row = await scope.Database.SingleOrDefaultByIdAsync<OrderRecord>(id);
        return row is null ? null : Map(row);
    }

    public async Task<Order?> GetByNumberAsync(string orderNumber)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var row = await scope.Database.FirstOrDefaultAsync<OrderRecord>(
            "WHERE OrderNumber = @0", orderNumber);
        return row is null ? null : Map(row);
    }

    public async Task<bool> TryMarkPaidAsync(int orderId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var affected = await scope.Database.ExecuteAsync(
            "UPDATE Orders SET Status = @0, PaidAt = @1 WHERE Id = @2 AND Status = @3",
            (int)OrderStatus.Paid, DateTime.UtcNow, orderId, (int)OrderStatus.Draft);
        return affected > 0;
    }

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
                r.BillingType ?? 0, r.BillingFirstName, r.BillingLastName, r.BillingCompany,
                r.BillingStreet, r.BillingAddressLine2,
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
            r.PaidAt,
            r.PayrexxGatewayId,
            r.FulfillmentPayload,
            r.PaymentSource is { } ps ? (PaymentSource)ps : null);
}
