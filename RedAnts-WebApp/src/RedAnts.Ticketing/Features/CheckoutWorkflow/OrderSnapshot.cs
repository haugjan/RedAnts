using System.Text.Json.Serialization;
using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.CheckoutWorkflow;

public enum CheckoutSource
{
    Checkout,
    Express,
    QuickBuy
}

public sealed record OrderSnapshotItem(
    int Kind, int EventId, int SeasonId, int TierId, decimal UnitPrice, int Quantity, string EventName, string CategoryName,
    int? OriginType = null, string? OriginCardUuid = null, int OriginCategory = 0)
{
    [JsonIgnore] public bool IsSeasonPass => Kind == (int)CartLineKind.SeasonPass;
    [JsonIgnore] public bool IsConversion => OriginType is not null && !string.IsNullOrEmpty(OriginCardUuid);
}

public sealed record OrderSnapshotAddOn(
    int Id, int SeasonId, string EventName, int TierId, string CategoryName, string Label, decimal Price, int Quantity);

public sealed record OrderSnapshot(
    List<OrderSnapshotItem> Items,
    List<OrderSnapshotAddOn> AddOns,
    bool SubscribeNewsletter,
    string NewsletterSource)
{
    public static OrderSnapshot FromCart(Cart cart, bool subscribeNewsletter, CheckoutSource source)
    {
        var items = cart.Items
            .Select(i => new OrderSnapshotItem((int)i.Kind, i.EventId, i.SeasonId, i.TierId, i.UnitPrice, i.Quantity, i.EventName, i.CategoryName,
                i.Origin is { } o ? (int)o.CardType : null, i.Origin?.CardUuid.ToString(), i.Origin?.Category ?? 0))
            .ToList();
        var addOns = cart.Items
            .Where(i => i.Kind == CartLineKind.SeasonPass && i.AddOns.Count > 0)
            .SelectMany(i => i.AddOns.Select(a => new OrderSnapshotAddOn(a.Id, i.SeasonId, i.EventName, i.TierId, i.CategoryName, a.Label, a.Price, i.Quantity)))
            .Concat(cart.OrderAddOns.Select(a => new OrderSnapshotAddOn(a.Id, a.SeasonId, a.SeasonName, 0, "", a.Label, a.Price, 1)))
            .ToList();
        return new OrderSnapshot(items, addOns, subscribeNewsletter, source.ToString());
    }

    [JsonIgnore] public IReadOnlyList<int> EventIds => Items.Where(i => !i.IsSeasonPass).Select(i => i.EventId).Distinct().ToList();

    [JsonIgnore] public IReadOnlyList<int> SeasonIds => Items.Where(i => i.IsSeasonPass).Select(i => i.SeasonId).Distinct().ToList();

    public IReadOnlyList<TierDemand> EventDemand(int eventId) => Items
        .Where(i => !i.IsSeasonPass && i.EventId == eventId)
        .Select(i => new TierDemand(i.TierId, i.Quantity, i.IsConversion))
        .ToList();

    public IReadOnlyList<TierDemand> PassDemand(int seasonId) => Items
        .Where(i => i.IsSeasonPass && i.SeasonId == seasonId)
        .Select(i => new TierDemand(i.TierId, i.Quantity))
        .ToList();

    [JsonIgnore] public bool IsQuickBuy => NewsletterSource == CheckoutSource.QuickBuy.ToString();

    public static OrderSnapshot? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return System.Text.Json.JsonSerializer.Deserialize<OrderSnapshot>(json); }
        catch (System.Text.Json.JsonException) { return null; }
    }
}
