namespace RedAnts.Domain.Ticketing.Sales;

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
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => decimal.Round(UnitPrice * Quantity, 2);

    private OrderItem(int id, int orderId, OrderItemKind kind, Guid? articleGuid, int refId,
        TicketCategory category, string label, int quantity, decimal unitPrice)
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
        string label, int quantity, decimal unitPrice, Guid? articleGuid = null)
    {
        if (quantity < 1) throw new DomainException("Menge muss mindestens 1 sein.");
        if (unitPrice < 0) throw new DomainException("Preis darf nicht negativ sein.");
        return new OrderItem(0, orderId, kind, articleGuid, refId, category,
            (label ?? "").Trim(), quantity, decimal.Round(unitPrice, 2));
    }

    public static OrderItem FromPersistence(int id, int orderId, OrderItemKind kind, Guid? articleGuid,
        int refId, TicketCategory category, string label, int quantity, decimal unitPrice) =>
        new(id, orderId, kind, articleGuid, refId, category, label ?? "", quantity, unitPrice);
}
