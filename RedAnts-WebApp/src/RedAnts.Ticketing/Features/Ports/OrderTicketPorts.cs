namespace RedAnts.Ticketing.Features.Ports;

public interface IOrderTickets
{
    Task<int> DeactivateByOrderAsync(int orderId);
}
