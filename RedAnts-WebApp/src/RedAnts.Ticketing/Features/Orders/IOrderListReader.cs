namespace RedAnts.Ticketing.Features.Orders;

public interface IOrderListReader
{
    Task<IReadOnlyList<OrderListRow>> GetBySeasonAsync(int seasonId, IReadOnlyCollection<int> eventIds);
}
