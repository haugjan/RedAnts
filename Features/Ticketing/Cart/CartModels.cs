using System.Text.Json.Serialization;
using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Cart;

public sealed class CartItem
{
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public TicketCategory Category { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    [JsonIgnore] public string Key => $"{EventId}:{(int)Category}";
    [JsonIgnore] public decimal LineTotal => UnitPrice * Quantity;
}

public sealed class Cart
{
    public List<CartItem> Items { get; set; } = [];

    [JsonIgnore] public bool IsEmpty => Items.Count == 0;
    [JsonIgnore] public int TotalQuantity => Items.Sum(i => i.Quantity);
    [JsonIgnore] public decimal TotalAmount => Items.Sum(i => i.LineTotal);
}

public interface ICartService
{
    Cart Get();
    void Add(int eventId, string eventName, TicketCategory category, string categoryName, decimal unitPrice, int quantity);
    void SetQuantity(string key, int quantity);
    void Remove(string key);
    void Clear();
}
