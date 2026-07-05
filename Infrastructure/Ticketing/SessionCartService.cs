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

    public void Add(int eventId, string eventName, TicketCategory category, string categoryName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0) return;
        var cart = Get();
        var existing = cart.Items.FirstOrDefault(i => i.EventId == eventId && i.Category == category);
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
                EventId = eventId,
                EventName = eventName,
                Category = category,
                CategoryName = categoryName,
                UnitPrice = unitPrice,
                Quantity = Math.Min(quantity, MaxQuantityPerItem)
            });
        }
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
