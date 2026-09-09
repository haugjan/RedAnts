namespace RedAnts.Ticketing.Features.Orders;

public static class GetOrderDetail
{
    public sealed record Query(int OrderId);

    public sealed class Handler(IOrderDetailReader orders)
    {
        public async Task<OrderDetail> HandleAsync(Query query) =>
            await orders.GetAsync(query.OrderId) ?? throw new DomainException("Bestellung wurde nicht gefunden.");
    }
}
