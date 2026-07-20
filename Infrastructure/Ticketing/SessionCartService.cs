using System.Text.Json;
using Microsoft.AspNetCore.Http;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Cart;

namespace RedAnts.Infrastructure.Ticketing;

public sealed class SessionCartService(IHttpContextAccessor httpContextAccessor) : ICartService
{
    private const string SessionKey = "RedAnts.Cart";
    private const int MaxQuantityPerItem = 50;

    private ISession Session =>
        httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("No HTTP session available for the cart.");

    public Cart Get()
    {
        var json = Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json)) return new Cart();
        return JsonSerializer.Deserialize<Cart>(json) ?? new Cart();
    }

    public void Add(int eventId, string eventName, int tierId, string categoryName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0) return;
        var cart = Get();
        var existing = cart.Items.FirstOrDefault(i =>
            i.Kind == CartItemKind.EventTicket && i.EventId == eventId && i.TierId == tierId);
        if (existing is not null)
        {
            existing.Quantity = Math.Min(existing.Quantity + quantity, MaxQuantityPerItem);
            existing.UnitPrice = unitPrice;
            existing.EventName = eventName;
            existing.CategoryName = categoryName;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                Kind = CartItemKind.EventTicket,
                EventId = eventId,
                EventName = eventName,
                TierId = tierId,
                CategoryName = categoryName,
                UnitPrice = unitPrice,
                Quantity = Math.Min(quantity, MaxQuantityPerItem)
            });
        }
        Save(cart);
    }

    public void AddSeasonPass(int seasonId, string seasonName, int tierId, string categoryName, decimal unitPrice, int quantity, IReadOnlyList<CartAddOn> addOns)
    {
        if (quantity <= 0) return;
        var cart = Get();
        var addOnList = (addOns ?? [])
            .Select(a => new CartAddOn { Id = a.Id, Label = a.Label, Price = a.Price, SeasonId = a.SeasonId, SeasonName = a.SeasonName })
            .ToList();
        var addOnKey = addOnList.Count == 0 ? "" : string.Join("-", addOnList.Select(a => a.Id).OrderBy(x => x));
        var existing = cart.Items.FirstOrDefault(i =>
            i.Kind == CartItemKind.SeasonPass && i.SeasonId == seasonId && i.TierId == tierId && i.AddOnKey == addOnKey);
        if (existing is not null)
        {
            existing.Quantity = Math.Min(existing.Quantity + quantity, MaxQuantityPerItem);
            existing.UnitPrice = unitPrice;
            existing.EventName = seasonName;
            existing.CategoryName = categoryName;
            existing.AddOns = addOnList;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                Kind = CartItemKind.SeasonPass,
                SeasonId = seasonId,
                EventName = seasonName,
                TierId = tierId,
                CategoryName = categoryName,
                UnitPrice = unitPrice,
                Quantity = Math.Min(quantity, MaxQuantityPerItem),
                AddOns = addOnList
            });
        }
        Save(cart);
    }

    public void AddOrderAddOns(IReadOnlyList<CartAddOn> addOns)
    {
        if (addOns is null || addOns.Count == 0) return;
        var cart = Get();
        foreach (var a in addOns)
        {
            if (cart.OrderAddOns.Any(x => x.Id == a.Id)) continue;
            cart.OrderAddOns.Add(new CartAddOn { Id = a.Id, Label = a.Label, Price = a.Price, SeasonId = a.SeasonId, SeasonName = a.SeasonName });
        }
        Save(cart);
    }

    public void RemoveOrderAddOn(int addOnId)
    {
        var cart = Get();
        cart.OrderAddOns.RemoveAll(a => a.Id == addOnId);
        Save(cart);
    }

    public void SetQuantity(string key, int quantity)
    {
        var cart = Get();
        var item = cart.Items.FirstOrDefault(i => i.Key == key);
        if (item is null) return;
        if (quantity <= 0) cart.Items.Remove(item);
        else item.Quantity = Math.Min(quantity, MaxQuantityPerItem);
        Save(cart);
    }

    public void Remove(string key)
    {
        var cart = Get();
        cart.Items.RemoveAll(i => i.Key == key);
        Save(cart);
    }

    public void Clear() => Session.Remove(SessionKey);

    private void Save(Cart cart) => Session.SetString(SessionKey, JsonSerializer.Serialize(cart));
}
