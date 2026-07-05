using System.Text.Json.Serialization;
using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Cart;

public enum CartItemKind
{
    EventTicket,
    SeasonPass
}

public sealed class CartItem
{
    public CartItemKind Kind { get; set; } = CartItemKind.EventTicket;
    public int EventId { get; set; }
    public int SeasonId { get; set; }
    public string EventName { get; set; } = "";
    public TicketCategory Category { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    [JsonIgnore] public int RefId => Kind == CartItemKind.SeasonPass ? SeasonId : EventId;
    [JsonIgnore] public string Key => $"{(int)Kind}:{RefId}:{(int)Category}";
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
    void AddSeasonPass(int seasonId, string seasonName, TicketCategory category, string categoryName, decimal unitPrice, int quantity);
    void SetQuantity(string key, int quantity);
    void Remove(string key);
    void Clear();
}
