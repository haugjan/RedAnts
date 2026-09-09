using RedAnts.Ticketing.Features.Orders.Admin;

namespace RedAnts.Ticketing.Features.Orders;

public interface IOrderAddOnAdminReport
{
    Task<IReadOnlyList<AddOnDeliveryItem>> GetBySeasonAsync(int seasonId);
    Task SetDeliveredAsync(int orderAddOnId, bool delivered);
}
