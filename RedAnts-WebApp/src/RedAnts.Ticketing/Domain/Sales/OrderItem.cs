namespace RedAnts.Ticketing.Domain.Sales;

public sealed class OrderItem
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public OrderItemKind Kind { get; private set; }
    public Guid? ArticleGuid { get; private set; }
    public int RefId { get; private set; }
    public TicketCategory Category { get; private set; }
    public string Label { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }

    public Money LineTotal => UnitPrice.Times(Quantity);

    private OrderItem(int id, int orderId, OrderItemKind kind, Guid? articleGuid, int refId,
        TicketCategory category, string label, int quantity, Money unitPrice)
    {
        Id = id;
        OrderId = orderId;
        Kind = kind;
        ArticleGuid = articleGuid;
        RefId = refId;
        Category = category;
        Label = label;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public static OrderItem Create(int orderId, OrderItemKind kind, int refId, TicketCategory category,
        string label, int quantity, Money unitPrice, Guid? articleGuid = null)
    {
        if (quantity < 1) throw new DomainException("Menge muss mindestens 1 sein.");
        return new OrderItem(0, orderId, kind, articleGuid, refId, category,
            (label ?? "").Trim(), quantity, Money.Of(unitPrice.Amount, unitPrice.Currency, "unitPrice", "Preis"));
    }

    public static OrderItem FromPersistence(int id, int orderId, OrderItemKind kind, Guid? articleGuid,
        int refId, TicketCategory category, string label, int quantity, decimal unitPrice) =>
        new(id, orderId, kind, articleGuid, refId, category, label ?? "", quantity, Money.Stored(unitPrice));
}
