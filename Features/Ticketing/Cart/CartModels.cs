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
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = "";
}

public sealed class CartItem
{
    public CartItemKind Kind { get; set; } = CartItemKind.EventTicket;
    public int EventId { get; set; }
    public int SeasonId { get; set; }
    public string EventName { get; set; } = "";
    public int TierId { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public List<CartAddOn> AddOns { get; set; } = [];

    [JsonIgnore] public int RefId => Kind == CartItemKind.SeasonPass ? SeasonId : EventId;
    [JsonIgnore] public string AddOnKey => AddOns.Count == 0 ? "" : string.Join("-", AddOns.Select(a => a.Id).OrderBy(x => x));
    [JsonIgnore] public string Key => $"{(int)Kind}:{RefId}:{TierId}:{AddOnKey}";
    [JsonIgnore] public decimal AddOnTotal => AddOns.Sum(a => a.Price);
    [JsonIgnore] public decimal LineTotal => (UnitPrice + AddOnTotal) * Quantity;
}

public sealed class Cart
{
    public List<CartItem> Items { get; set; } = [];
    public List<CartAddOn> OrderAddOns { get; set; } = [];

    [JsonIgnore] public bool IsEmpty => Items.Count == 0 && OrderAddOns.Count == 0;
    [JsonIgnore] public int TotalQuantity => Items.Sum(i => i.Quantity) + OrderAddOns.Count;
    [JsonIgnore] public decimal TotalAmount => Items.Sum(i => i.LineTotal) + OrderAddOns.Sum(a => a.Price);
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
    void Add(int eventId, string eventName, int tierId, string categoryName, decimal unitPrice, int quantity);
    void AddSeasonPass(int seasonId, string seasonName, int tierId, string categoryName, decimal unitPrice, int quantity, IReadOnlyList<CartAddOn> addOns);
    void AddOrderAddOns(IReadOnlyList<CartAddOn> addOns);
    void RemoveOrderAddOn(int addOnId);
    void SetQuantity(string key, int quantity);
    void Remove(string key);
    void Clear();
}
