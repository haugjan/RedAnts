using Microsoft.Extensions.Configuration;
using NPoco;
using RedAnts.Domain;
using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;
using Umbraco.Cms.Infrastructure.Scoping;
using PaymentMethod = RedAnts.Ticketing.Domain.Sales.PaymentMethod;

namespace RedAnts.Ticketing.Infrastructure.Sales;

public sealed class OrderRepository(IScopeProvider scopeProvider, IConfiguration config) : IOrders
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
            (int)OrderStatus.Paid, SwissTime.Timestamp, orderId, (int)OrderStatus.Draft);
        return affected > 0;
    }

    public async Task<bool> TryCancelDraftAsync(int orderId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var affected = await scope.Database.ExecuteAsync(
            "UPDATE Orders SET Status = @0 WHERE Id = @1 AND Status = @2",
            (int)OrderStatus.Cancelled, orderId, (int)OrderStatus.Draft);
        return affected > 0;
    }

    public async Task<IReadOnlyList<Order>> GetDraftsCreatedBetweenAsync(DateTimeOffset createdAfter, DateTimeOffset createdBefore)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<OrderRecord>(
            "WHERE Status = @0 AND CreatedAt >= @1 AND CreatedAt < @2 ORDER BY Id", (int)OrderStatus.Draft, createdAfter, createdBefore);
        return rows.Select(Map).ToList();
    }

    public async Task CopyBillingToTicketsAsync(int orderId)
    {
        if (orderId <= 0) return;
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await CopyAsync(scope.Database, "EventTickets", "Email", orderId);
        await CopyAsync(scope.Database, "SeasonPasses", "BuyerEmail", orderId);
        await CopyAsync(scope.Database, "SeasonSingleTickets", "BuyerEmail", orderId);
    }

    private static Task CopyAsync(IDatabase db, string table, string emailColumn, int orderId) =>
        db.ExecuteAsync($@"
            UPDATE t SET
                BuyerType = COALESCE(t.BuyerType, o.BillingType),
                BuyerFirstName = COALESCE(t.BuyerFirstName, o.BillingFirstName),
                BuyerLastName = COALESCE(t.BuyerLastName, o.BillingLastName),
                BuyerCompany = COALESCE(t.BuyerCompany, o.BillingCompany),
                {emailColumn} = COALESCE(t.{emailColumn}, o.BillingEmail),
                Street = COALESCE(t.Street, o.BillingStreet),
                AddressLine2 = COALESCE(t.AddressLine2, o.BillingAddressLine2),
                PostalCode = COALESCE(t.PostalCode, o.BillingPostalCode),
                City = COALESCE(t.City, o.BillingCity),
                Country = COALESCE(t.Country, o.BillingCountry),
                Phone = COALESCE(t.Phone, o.BillingPhone)
            FROM {table} t INNER JOIN Orders o ON o.Id = t.OrderId
            WHERE t.OrderId = @0", orderId);

    public async Task<string> NextOrderNumberAsync()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var seq = await scope.Database.ExecuteScalarAsync<long>("SELECT NEXT VALUE FOR OrderNumberSeq");
        var prefix = config["Orders:NumberPrefix"] ?? "";
        var year = SwissTime.Now.Year;
        return $"{prefix}{year}-{seq:000000}";
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
