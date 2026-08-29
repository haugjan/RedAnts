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

    public void Add(int eventId, string eventName, int tierId, string categoryName, string standardCategoryName, decimal unitPrice, int quantity)
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
            existing.StandardCategoryName = standardCategoryName;
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
                StandardCategoryName = standardCategoryName,
                UnitPrice = unitPrice,
                Quantity = Math.Min(quantity, MaxQuantityPerItem)
            });
        }
        Save(cart);
    }

    public void AddSeasonPass(int seasonId, string seasonName, int tierId, string categoryName, string standardCategoryName, decimal unitPrice, int quantity, IReadOnlyList<CartAddOn> addOns)
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
            existing.StandardCategoryName = standardCategoryName;
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
                StandardCategoryName = standardCategoryName,
                UnitPrice = unitPrice,
                Quantity = Math.Min(quantity, MaxQuantityPerItem),
                AddOns = addOnList
            });
        }
        Save(cart);
    }

    public int AddConversion(int eventId, string eventName, int seasonId, int tierId, string categoryName,
        int originCategory, decimal unitPrice, TicketType originType, Guid originCardUuid, string originLabel, int capTotal)
    {
        var cardKey = originCardUuid.ToString();
        var cart = Get();
        var existing = cart.Items.FirstOrDefault(i =>
            i.Kind == CartItemKind.EventTicket && i.EventId == eventId && i.OriginCardUuid == cardKey);
        var inCart = existing?.Quantity ?? 0;
        var allowed = Math.Min(capTotal, MaxQuantityPerItem);
        if (inCart >= allowed) return 0;

        if (existing is not null)
        {
            existing.Quantity = inCart + 1;
            existing.UnitPrice = unitPrice;
            existing.EventName = eventName;
            existing.CategoryName = categoryName;
            existing.OriginCap = allowed;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                Kind = CartItemKind.EventTicket,
                EventId = eventId,
                SeasonId = seasonId,
                EventName = eventName,
                TierId = tierId,
                CategoryName = categoryName,
                StandardCategoryName = categoryName,
                UnitPrice = unitPrice,
                Quantity = 1,
                OriginType = (int)originType,
                OriginCardUuid = cardKey,
                OriginLabel = originLabel,
                OriginCategory = originCategory,
                OriginCap = allowed
            });
        }
        Save(cart);
        return 1;
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
        if (item.IsConversion)
            quantity = Math.Min(quantity, item.OriginCap > 0 ? item.OriginCap : 1);
        if (quantity <= 0) cart.Items.Remove(item);
        else item.Quantity = Math.Min(quantity, MaxQuantityPerItem);
        PruneOrphanedOrderAddOns(cart);
        Save(cart);
    }

    public void Remove(string key)
    {
        var cart = Get();
        cart.Items.RemoveAll(i => i.Key == key);
        PruneOrphanedOrderAddOns(cart);
        Save(cart);
    }

    public void Clear() => Session.Remove(SessionKey);

    private static void PruneOrphanedOrderAddOns(Cart cart)
    {
        if (cart.OrderAddOns.Count == 0) return;
        var seasonsWithPass = cart.Items
            .Where(i => i.Kind == CartItemKind.SeasonPass)
            .Select(i => i.SeasonId)
            .ToHashSet();
        cart.OrderAddOns.RemoveAll(a => !seasonsWithPass.Contains(a.SeasonId));
    }

    private void Save(Cart cart) => Session.SetString(SessionKey, JsonSerializer.Serialize(cart));
}
