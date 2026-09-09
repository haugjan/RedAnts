namespace RedAnts.Ticketing.Features.Orders;

public interface IOrderAddOnListReader
{
    Task<IReadOnlyList<OrderAddOnRow>> GetBySeasonAsync(int seasonId);
}
