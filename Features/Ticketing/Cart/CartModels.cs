using System.Text.Json.Serialization;
using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Cart;

public enum CartItemKind
{
    EventTicket,
    SeasonPass
}

public sealed class CartAddOn
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
    public decimal Price { get; set; }
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
    public List<CartAddOn> AddOns { get; set; } = [];

    [JsonIgnore] public int RefId => Kind == CartItemKind.SeasonPass ? SeasonId : EventId;
    [JsonIgnore] public string AddOnKey => AddOns.Count == 0 ? "" : string.Join("-", AddOns.Select(a => a.Id).OrderBy(x => x));
    [JsonIgnore] public string Key => $"{(int)Kind}:{RefId}:{(int)Category}:{AddOnKey}";
    [JsonIgnore] public decimal AddOnTotal => AddOns.Sum(a => a.Price);
    [JsonIgnore] public decimal LineTotal => (UnitPrice + AddOnTotal) * Quantity;
}

public sealed class Cart
{
    public List<CartItem> Items { get; set; } = [];

    [JsonIgnore] public bool IsEmpty => Items.Count == 0;
    [JsonIgnore] public int TotalQuantity => Items.Sum(i => i.Quantity);
    [JsonIgnore] public decimal TotalAmount => Items.Sum(i => i.LineTotal);
}

public static class ExpressCheckout
{
    public const decimal MaxAmount = 50m;

    public static bool IsAllowed(Cart cart) =>
        !cart.IsEmpty
        && cart.TotalAmount < MaxAmount
        && cart.Items.All(i => i.Kind != CartItemKind.SeasonPass);
}

public interface ICartService
{
    Cart Get();
    void Add(int eventId, string eventName, TicketCategory category, string categoryName, decimal unitPrice, int quantity);
    void AddSeasonPass(int seasonId, string seasonName, TicketCategory category, string categoryName, decimal unitPrice, int quantity, IReadOnlyList<CartAddOn> addOns);
    void SetQuantity(string key, int quantity);
    void Remove(string key);
    void Clear();
}
