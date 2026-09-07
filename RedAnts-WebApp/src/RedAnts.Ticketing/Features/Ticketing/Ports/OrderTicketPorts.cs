namespace RedAnts.Features.Ticketing.Ports;

public interface IOrderTickets
{
    Task<int> DeactivateByOrderAsync(int orderId);
}
