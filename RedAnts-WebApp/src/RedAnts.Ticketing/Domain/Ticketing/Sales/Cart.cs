namespace RedAnts.Domain.Ticketing.Sales;

public enum CartLineKind
{
    EventTicket,
    SeasonPass
}

public sealed record CartAddOn(int Id, string Label, decimal Price, int SeasonId, string SeasonName, bool RequiresMobileNumber = false);

public sealed record ConversionOrigin(TicketType CardType, Guid CardUuid, string Label, int Category, int Cap);

public sealed class CartLine
{
    public CartLineKind Kind { get; }
    public int EventId { get; }
    public int SeasonId { get; }
    public string EventName { get; private set; }
    public int TierId { get; }
    public string CategoryName { get; private set; }
    public string StandardCategoryName { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; internal set; }
    public IReadOnlyList<CartAddOn> AddOns { get; private set; }
    public ConversionOrigin? Origin { get; private set; }

    internal CartLine(CartLineKind kind, int eventId, int seasonId, string eventName, int tierId, string categoryName,
        string standardCategoryName, decimal unitPrice, int quantity, IReadOnlyList<CartAddOn> addOns, ConversionOrigin? origin)
    {
        Kind = kind;
        EventId = eventId;
        SeasonId = seasonId;
        EventName = eventName;
        TierId = tierId;
        CategoryName = categoryName;
        StandardCategoryName = standardCategoryName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        AddOns = addOns;
        Origin = origin;
    }

    public static CartLine FromPersistence(CartLineKind kind, int eventId, int seasonId, string eventName, int tierId,
        string categoryName, string standardCategoryName, decimal unitPrice, int quantity,
        IReadOnlyList<CartAddOn>? addOns = null, ConversionOrigin? origin = null) =>
        new(kind, eventId, seasonId, eventName ?? "", tierId, categoryName ?? "", standardCategoryName ?? "",
            unitPrice, Math.Max(1, quantity), addOns ?? [], origin);

    public bool IsConversion => Origin is not null;
    public int OriginCap => Origin?.Cap ?? 0;
    public string? OriginLabel => Origin?.Label;
    public int RefId => Kind == CartLineKind.SeasonPass ? SeasonId : EventId;
    public string AddOnKey => AddOns.Count == 0 ? "" : string.Join("-", AddOns.Select(a => a.Id).OrderBy(x => x));
    public string Key => $"{(int)Kind}:{RefId}:{TierId}:{AddOnKey}" + (Origin is null ? "" : ":" + Origin.CardUuid);
    public decimal AddOnTotal => AddOns.Sum(a => a.Price);
    public decimal LineTotal => (UnitPrice + AddOnTotal) * Quantity;

    internal void Refresh(string eventName, string categoryName, string standardCategoryName, decimal unitPrice)
    {
        EventName = eventName;
        CategoryName = categoryName;
        StandardCategoryName = standardCategoryName;
        UnitPrice = unitPrice;
    }

    internal void ReplaceAddOns(IReadOnlyList<CartAddOn> addOns) => AddOns = addOns;

    internal void RefreshOrigin(ConversionOrigin origin) => Origin = origin;
}

public sealed record TierDemand(int TierId, int Quantity, bool IsConversion = false);

public sealed class Cart
{
    public const int MaxQuantityPerLine = 50;
    public const decimal ExpressLimit = 50m;

    private readonly List<CartLine> _items;
    private readonly List<CartAddOn> _orderAddOns;

    private Cart(List<CartLine> items, List<CartAddOn> orderAddOns)
    {
        _items = items;
        _orderAddOns = orderAddOns;
    }

    public static Cart Empty() => new([], []);

    public static Cart FromPersistence(IEnumerable<CartLine> items, IEnumerable<CartAddOn> orderAddOns) =>
        new(items.ToList(), orderAddOns.ToList());

    public IReadOnlyList<CartLine> Items => _items;
    public IReadOnlyList<CartAddOn> OrderAddOns => _orderAddOns;

    public bool IsEmpty => _items.Count == 0 && _orderAddOns.Count == 0;
    public int TotalQuantity => _items.Sum(i => i.Quantity) + _orderAddOns.Count;
    public decimal TotalAmount => _items.Sum(i => i.LineTotal) + _orderAddOns.Sum(a => a.Price);
    public bool QualifiesForExpress => !IsEmpty && TotalAmount < ExpressLimit && _items.All(i => i.Kind != CartLineKind.SeasonPass);
    public bool RequiresMobileNumber => _items.SelectMany(i => i.AddOns).Concat(_orderAddOns).Any(a => a.RequiresMobileNumber);
    public IReadOnlyList<int> EventIds => _items.Where(i => i.Kind == CartLineKind.EventTicket).Select(i => i.EventId).Distinct().ToList();
    public IReadOnlyList<int> SeasonIds => _items.Where(i => i.Kind == CartLineKind.SeasonPass).Select(i => i.SeasonId).Distinct().ToList();
    public bool HasRegularTicketsFor(int eventId) => _items.Any(i => i.Kind == CartLineKind.EventTicket && i.EventId == eventId && !i.IsConversion);

    public IReadOnlyList<TierDemand> EventDemand(int eventId) => _items
        .Where(i => i.Kind == CartLineKind.EventTicket && i.EventId == eventId)
        .Select(i => new TierDemand(i.TierId, i.Quantity, i.IsConversion))
        .ToList();

    public IReadOnlyList<TierDemand> PassDemand(int seasonId) => _items
        .Where(i => i.Kind == CartLineKind.SeasonPass && i.SeasonId == seasonId)
        .Select(i => new TierDemand(i.TierId, i.Quantity))
        .ToList();

    public void AddEventTickets(int eventId, string eventName, int tierId, string categoryName, string standardCategoryName, decimal unitPrice, int quantity)
    {
        RequirePositive(quantity);
        var existing = _items.FirstOrDefault(i => i.Kind == CartLineKind.EventTicket && i.EventId == eventId && i.TierId == tierId && !i.IsConversion);
        if (existing is not null)
        {
            existing.Quantity = Math.Min(existing.Quantity + quantity, MaxQuantityPerLine);
            existing.Refresh(eventName, categoryName, standardCategoryName, unitPrice);
            return;
        }
        _items.Add(new CartLine(CartLineKind.EventTicket, eventId, 0, eventName, tierId, categoryName, standardCategoryName,
            unitPrice, Math.Min(quantity, MaxQuantityPerLine), [], null));
    }

    public void AddSeasonPasses(int seasonId, string seasonName, int tierId, string categoryName, string standardCategoryName,
        decimal unitPrice, int quantity, IReadOnlyList<CartAddOn> addOns)
    {
        RequirePositive(quantity);
        var addOnList = (addOns ?? []).ToList();
        var addOnKey = addOnList.Count == 0 ? "" : string.Join("-", addOnList.Select(a => a.Id).OrderBy(x => x));
        var existing = _items.FirstOrDefault(i => i.Kind == CartLineKind.SeasonPass && i.SeasonId == seasonId && i.TierId == tierId && i.AddOnKey == addOnKey);
        if (existing is not null)
        {
            existing.Quantity = Math.Min(existing.Quantity + quantity, MaxQuantityPerLine);
            existing.Refresh(seasonName, categoryName, standardCategoryName, unitPrice);
            existing.ReplaceAddOns(addOnList);
            return;
        }
        _items.Add(new CartLine(CartLineKind.SeasonPass, 0, seasonId, seasonName, tierId, categoryName, standardCategoryName,
            unitPrice, Math.Min(quantity, MaxQuantityPerLine), addOnList, null));
    }

    public int AddConversion(int eventId, string eventName, int seasonId, int tierId, string categoryName, decimal unitPrice, ConversionOrigin origin)
    {
        var allowed = Math.Min(origin.Cap, MaxQuantityPerLine);
        var existing = _items.FirstOrDefault(i => i.Kind == CartLineKind.EventTicket && i.EventId == eventId && i.Origin?.CardUuid == origin.CardUuid);
        var inCart = existing?.Quantity ?? 0;
        if (inCart >= allowed) return 0;

        if (existing is not null)
        {
            existing.Quantity = inCart + 1;
            existing.Refresh(eventName, categoryName, categoryName, unitPrice);
            existing.RefreshOrigin(origin with { Cap = allowed });
            return 1;
        }
        _items.Add(new CartLine(CartLineKind.EventTicket, eventId, seasonId, eventName, tierId, categoryName, categoryName,
            unitPrice, 1, [], origin with { Cap = allowed }));
        return 1;
    }

    public void AddOrderAddOns(IEnumerable<CartAddOn> addOns)
    {
        foreach (var addOn in addOns)
            if (_orderAddOns.All(a => a.Id != addOn.Id))
                _orderAddOns.Add(addOn);
    }

    public void RemoveOrderAddOn(int addOnId) => _orderAddOns.RemoveAll(a => a.Id == addOnId);

    public void SetQuantity(string key, int quantity)
    {
        var line = _items.FirstOrDefault(i => i.Key == key);
        if (line is null) return;
        if (line.IsConversion) quantity = Math.Min(quantity, line.OriginCap > 0 ? line.OriginCap : 1);
        if (quantity <= 0) _items.Remove(line);
        else line.Quantity = Math.Min(quantity, MaxQuantityPerLine);
        PruneOrphanedOrderAddOns();
    }

    public void Remove(string key)
    {
        _items.RemoveAll(i => i.Key == key);
        PruneOrphanedOrderAddOns();
    }

    public void Clear()
    {
        _items.Clear();
        _orderAddOns.Clear();
    }

    private void PruneOrphanedOrderAddOns()
    {
        if (_orderAddOns.Count == 0) return;
        var seasonsWithPass = _items.Where(i => i.Kind == CartLineKind.SeasonPass).Select(i => i.SeasonId).ToHashSet();
        _orderAddOns.RemoveAll(a => !seasonsWithPass.Contains(a.SeasonId));
    }

    private static void RequirePositive(int quantity)
    {
        if (quantity < 1) throw new DomainException("Menge muss mindestens 1 sein.");
    }
}
