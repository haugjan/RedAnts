namespace RedAnts.Ticketing.Features.Orders;

public interface IOrderDetailReader
{
    Task<OrderDetail?> GetAsync(int orderId);
}
