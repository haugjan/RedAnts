namespace RedAnts.Ticketing.Features.Orders;

public interface IOrderTickets
{
    Task<int> DeactivateByOrderAsync(int orderId);
}
