using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public sealed class AddOnDeliveryItem
{
    public int Id { get; init; }
    public int OrderId { get; init; }
    public string OrderNumber { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public OrderStatus OrderStatus { get; init; }
    public string BuyerName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Label { get; init; } = "";
    public string CategoryName { get; init; } = "";
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public bool Delivered { get; set; }
}

public interface IOrderAddOnAdminReport
{
    Task<IReadOnlyList<AddOnDeliveryItem>> GetBySeasonAsync(int seasonId);
    Task SetDeliveredAsync(int orderAddOnId, bool delivered);
}
