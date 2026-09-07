using System.Text.Json;
using Microsoft.AspNetCore.Http;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class SessionCartRepository(IHttpContextAccessor httpContextAccessor) : ICartRepository
{
    public const string SessionKey = "RedAnts.Cart";

    private ISession Session =>
        httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("No HTTP session available for the cart.");

    public Cart Load() => CartJson.Read(Session.GetString(SessionKey));

    public void Save(Cart cart) => Session.SetString(SessionKey, CartJson.Write(cart));

    public void Clear() => Session.Remove(SessionKey);
}

public static class CartJson
{
    public static Cart Read(string? json)
    {
        if (string.IsNullOrEmpty(json)) return Cart.Empty();
        var dto = JsonSerializer.Deserialize<CartDto>(json);
        return dto is null ? Cart.Empty() : ToCart(dto);
    }

    public static string Write(Cart cart) => JsonSerializer.Serialize(ToDto(cart));

    private static Cart ToCart(CartDto dto) =>
        Cart.FromPersistence(dto.Items.Select(ToLine), dto.OrderAddOns.Select(ToAddOn));

    private static CartLine ToLine(CartItemDto i) =>
        CartLine.FromPersistence((CartLineKind)i.Kind, i.EventId, i.SeasonId, i.EventName, i.TierId, i.CategoryName,
            i.StandardCategoryName, i.UnitPrice, i.Quantity, i.AddOns.Select(ToAddOn).ToList(), ToOrigin(i));

    private static ConversionOrigin? ToOrigin(CartItemDto i) =>
        i.OriginType is { } type && Guid.TryParse(i.OriginCardUuid, out var uuid)
            ? new ConversionOrigin((TicketType)type, uuid, i.OriginLabel ?? "", i.OriginCategory, i.OriginCap)
            : null;

    private static CartAddOn ToAddOn(CartAddOnDto a) =>
        new(a.Id, a.Label, a.Price, a.SeasonId, a.SeasonName, a.RequiresMobileNumber);

    private static CartDto ToDto(Cart cart) => new()
    {
        Items = cart.Items.Select(ToDto).ToList(),
        OrderAddOns = cart.OrderAddOns.Select(ToDto).ToList()
    };

    private static CartItemDto ToDto(CartLine line) => new()
    {
        Kind = (int)line.Kind,
        EventId = line.EventId,
        SeasonId = line.SeasonId,
        EventName = line.EventName,
        TierId = line.TierId,
        CategoryName = line.CategoryName,
        StandardCategoryName = line.StandardCategoryName,
        UnitPrice = line.UnitPrice,
        Quantity = line.Quantity,
        AddOns = line.AddOns.Select(ToDto).ToList(),
        OriginType = line.Origin is { } o ? (int)o.CardType : null,
        OriginCardUuid = line.Origin?.CardUuid.ToString(),
        OriginLabel = line.Origin?.Label,
        OriginCategory = line.Origin?.Category ?? 0,
        OriginCap = line.Origin?.Cap ?? 0
    };

    private static CartAddOnDto ToDto(CartAddOn a) => new()
    {
        Id = a.Id,
        Label = a.Label,
        Price = a.Price,
        SeasonId = a.SeasonId,
        SeasonName = a.SeasonName,
        RequiresMobileNumber = a.RequiresMobileNumber
    };

    public sealed class CartDto
    {
        public List<CartItemDto> Items { get; set; } = [];
        public List<CartAddOnDto> OrderAddOns { get; set; } = [];
    }

    public sealed class CartItemDto
    {
        public int Kind { get; set; }
        public int EventId { get; set; }
        public int SeasonId { get; set; }
        public string EventName { get; set; } = "";
        public int TierId { get; set; }
        public string CategoryName { get; set; } = "";
        public string StandardCategoryName { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public List<CartAddOnDto> AddOns { get; set; } = [];
        public int? OriginType { get; set; }
        public string? OriginCardUuid { get; set; }
        public string? OriginLabel { get; set; }
        public int OriginCategory { get; set; }
        public int OriginCap { get; set; }
    }

    public sealed class CartAddOnDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = "";
        public decimal Price { get; set; }
        public int SeasonId { get; set; }
        public string SeasonName { get; set; } = "";
        public bool RequiresMobileNumber { get; set; }
    }
}
