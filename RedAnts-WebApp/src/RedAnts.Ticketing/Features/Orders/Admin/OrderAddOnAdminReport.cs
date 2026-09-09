using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Orders.Admin;

public sealed class AddOnDeliveryItem
{
    public int Id { get; init; }
    public int OrderId { get; init; }
    public string OrderNumber { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public OrderStatus OrderStatus { get; init; }
    public string BuyerName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Label { get; init; } = "";
    public string CategoryName { get; init; } = "";
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public bool Delivered { get; set; }
}
