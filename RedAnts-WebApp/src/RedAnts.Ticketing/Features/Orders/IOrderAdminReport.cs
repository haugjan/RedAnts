using RedAnts.Ticketing.Features.Orders.Admin;

namespace RedAnts.Ticketing.Features.Orders;

public interface IOrderAdminReport
{
    Task<IReadOnlyList<OrderListItem>> GetBySeasonAsync(int seasonId, IReadOnlyCollection<int> eventIds);
}
