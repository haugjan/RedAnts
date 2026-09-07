using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Admission;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.AdmissionWorkflow;
using RedAnts.Features.Ticketing.Email;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Features.Ticketing.Scanning;
using RedAnts.Features.Ticketing.Tickets;

namespace RedAnts.Ticketing.Tests.CheckoutWorkflow;

internal sealed class InMemoryOrders : IOrders
{
    private int _nextId = 1;

    public List<Order> Stored { get; } = [];
    public int BillingCopies { get; private set; }

    public Task<Order> SaveAsync(Order order)
    {
        if (order.Id != 0)
        {
            Stored.RemoveAll(o => o.Id == order.Id);
            Stored.Add(order);
            return Task.FromResult(order);
        }
        var saved = Order.FromPersistence(_nextId++, order.OrderNumber, order.BillingAddress, order.Currency, order.SubtotalNet,
            order.VatRate, order.VatAmount, order.TotalGross, order.SellerUid, order.PaymentMethod, order.Status, order.CreatedAt,
            order.PaidAt, order.PayrexxGatewayId, order.FulfillmentPayload, order.PaymentSource);
        Stored.Add(saved);
        return Task.FromResult(saved);
    }

    public Task<string> NextOrderNumberAsync() => Task.FromResult($"T-{_nextId:000}");

    public Task<Order?> GetByIdAsync(int id) => Task.FromResult(Stored.FirstOrDefault(o => o.Id == id));

    public Task<Order?> GetByNumberAsync(string orderNumber) => Task.FromResult(Stored.FirstOrDefault(o => o.OrderNumber == orderNumber));

    public Task<bool> TryMarkPaidAsync(int orderId)
    {
        var order = Stored.FirstOrDefault(o => o.Id == orderId && o.Status == OrderStatus.Draft);
        order?.MarkPaid();
        return Task.FromResult(order is not null);
    }

    public Task<bool> TryCancelDraftAsync(int orderId)
    {
        var order = Stored.FirstOrDefault(o => o.Id == orderId && o.Status == OrderStatus.Draft);
        order?.Cancel();
        return Task.FromResult(order is not null);
    }

    public Task<IReadOnlyList<Order>> GetDraftsCreatedBetweenAsync(DateTime createdAfter, DateTime createdBefore) =>
        Task.FromResult<IReadOnlyList<Order>>(Stored.Where(o => o.Status == OrderStatus.Draft && o.CreatedAt >= createdAfter && o.CreatedAt < createdBefore).ToList());

    public Task CopyBillingToTicketsAsync(int orderId)
    {
        BillingCopies++;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryEventPrices : IEventPrices
{
    private readonly Dictionary<int, EventPrice> _prices = new();

    public Dictionary<int, CapacityUsage> Usage { get; } = new();
    public int FailingSaves { get; set; }
    public int Saves { get; private set; }

    public void Seed(EventPrice price) => _prices[price.EventId] = price;

    public EventPrice Stored(int eventId) => _prices[eventId];

    public Task<EventPrice?> GetByEventAsync(int eventId) =>
        Task.FromResult(_prices.TryGetValue(eventId, out var price) ? Copy(price, price.Version) : null);

    public Task<EventPrice> SaveAsync(EventPrice price) => throw new NotSupportedException();

    public Task DeleteAsync(int eventPriceId) => throw new NotSupportedException();

    public Task<CapacityUsage> GetUsageAsync(int eventId) =>
        Task.FromResult(Usage.TryGetValue(eventId, out var usage) ? usage : CapacityUsage.None);

    public Task SaveReservationAsync(EventPrice price)
    {
        Saves++;
        if (FailingSaves > 0)
        {
            FailingSaves--;
            throw new ConcurrencyException("contended");
        }
        var stored = _prices[price.EventId];
        if (stored.Version != price.Version) throw new ConcurrencyException("stale");
        _prices[price.EventId] = Copy(price, price.Version + 1);
        return Task.CompletedTask;
    }

    private static EventPrice Copy(EventPrice price, int version) =>
        EventPrice.FromPersistence(price.Id, price.EventId, price.TotalSalesQuota, price.AdmissionQuota,
            price.Categories.Select(c => CategoryPrice.FromPersistence(c.Category, c.SalePrice, c.Quota, c.AvailableUntil, c.TierId, c.Reserved)).ToList(),
            price.ConversionOnly, price.Reserved, version);
}

internal sealed class InMemorySeasonPrices : ISeasonPrices
{
    private readonly Dictionary<int, SeasonPrice> _prices = new();

    public Dictionary<int, CapacityUsage> Usage { get; } = new();
    public int FailingSaves { get; set; }

    public void Seed(SeasonPrice price) => _prices[price.SeasonId] = price;

    public SeasonPrice Stored(int seasonId) => _prices[seasonId];

    public Task<SeasonPrice?> GetBySeasonAsync(int seasonId) =>
        Task.FromResult(_prices.TryGetValue(seasonId, out var price) ? Copy(price, price.Version) : null);

    public Task<SeasonPrice> SaveAsync(SeasonPrice price) => throw new NotSupportedException();

    public Task DeleteAsync(int seasonPriceId) => throw new NotSupportedException();

    public Task<CapacityUsage> GetPassUsageAsync(int seasonId) =>
        Task.FromResult(Usage.TryGetValue(seasonId, out var usage) ? usage : CapacityUsage.None);

    public Task SaveReservationAsync(SeasonPrice price)
    {
        if (FailingSaves > 0)
        {
            FailingSaves--;
            throw new ConcurrencyException("contended");
        }
        var stored = _prices[price.SeasonId];
        if (stored.Version != price.Version) throw new ConcurrencyException("stale");
        _prices[price.SeasonId] = Copy(price, price.Version + 1);
        return Task.CompletedTask;
    }

    private static SeasonPrice Copy(SeasonPrice price, int version) =>
        SeasonPrice.FromPersistence(price.Id, price.SeasonId, price.TotalSalesQuota,
            price.Categories.Select(c => SeasonCategoryPrice.FromPersistence(c.Category, c.PassPrice, c.PassOffered, c.PassQuota, c.TicketPrice,
                c.TicketOffered, c.TicketQuota, c.PassAvailableFrom, c.PassAvailableUntil, c.TicketAvailableUntil, c.TierId, c.Reserved)).ToList(),
            price.DefaultTicketSalesQuota, price.Reserved, version);
}

internal sealed class RecordingOrderLog : IOrderLog
{
    public List<(int OrderId, OrderStatus Status, string? By, string? Note)> Entries { get; } = [];

    public Task AppendAsync(int orderId, OrderStatus toStatus, string? changedBy, string? note = null)
    {
        Entries.Add((orderId, toStatus, changedBy, note));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OrderLogEntry>> GetByOrderAsync(int orderId) =>
        Task.FromResult<IReadOnlyList<OrderLogEntry>>(Entries.Where(e => e.OrderId == orderId)
            .Select(e => new OrderLogEntry(e.Status, e.By, DateTime.UtcNow, e.Note)).ToList());
}

internal sealed class StubConversionRules : IEventConversionRules
{
    public HashSet<int> ConversionOnlyEvents { get; } = [];

    public Task<IReadOnlyList<EventConversionRule>> GetAllAsync() => Task.FromResult<IReadOnlyList<EventConversionRule>>([]);

    public Task<IReadOnlyList<EventConversionRule>> GetByEventAsync(int eventId) => Task.FromResult<IReadOnlyList<EventConversionRule>>([]);

    public Task SetAsync(int eventId, TicketType cardType, decimal? discount) => Task.CompletedTask;

    public Task<bool> GetConversionOnlyAsync(int eventId) => Task.FromResult(ConversionOnlyEvents.Contains(eventId));

    public Task SetConversionOnlyAsync(int eventId, bool value) => Task.CompletedTask;
}

internal sealed class StubAdmission : IOccupancyReader
{
    public Dictionary<int, Occupancy> Occupancies { get; } = new();

    public Task<Occupancy> GetAsync(int eventId) =>
        Task.FromResult(Occupancies.TryGetValue(eventId, out var occupancy) ? occupancy : new Occupancy(0, null));
}

internal sealed class StubSeasonAddOns : ISeasonAddOns
{
    public List<SeasonAddOn> AddOns { get; } = [];

    public Task<IReadOnlyList<SeasonAddOn>> GetBySeasonAsync(int seasonId) =>
        Task.FromResult<IReadOnlyList<SeasonAddOn>>(AddOns.Where(a => a.SeasonId == seasonId).ToList());

    public Task ReplaceForSeasonAsync(int seasonId, IReadOnlyList<SeasonAddOn> options) => Task.CompletedTask;
}

internal sealed class StubPayrexx : IPayrexxGateway
{
    public bool Enabled { get; set; } = true;
    public bool FailGatewayCreation { get; set; }
    public PayrexxStatus Status { get; set; } = PayrexxStatus.Pending;
    public bool ThrowOnStatus { get; set; }
    public List<PayrexxCreateRequest> Requests { get; } = [];
    public int StatusCalls { get; private set; }

    public Task<PayrexxGatewayResult> CreateGatewayAsync(PayrexxCreateRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        if (FailGatewayCreation) throw new InvalidOperationException("payrexx down");
        return Task.FromResult(new PayrexxGatewayResult($"gw-{Requests.Count}", $"https://pay.test/{Requests.Count}"));
    }

    public Task<PayrexxStatus> GetGatewayStatusAsync(string gatewayId, CancellationToken cancellationToken = default)
    {
        StatusCalls++;
        if (ThrowOnStatus) throw new InvalidOperationException("payrexx down");
        return Task.FromResult(Status);
    }

    public Task<PayrexxRefundResult> RefundGatewayAsync(string gatewayId, int amountInCents, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

internal sealed class StubPublicBaseUrl : IPublicBaseUrl
{
    public string Resolve() => "https://tickets.test";

    public string TicketUrl(string token) => $"https://tickets.test/ticket/{token}";
}

internal sealed class StubOrderTokens : IOrderTokens
{
    public string Protect(int orderId) => $"tok{orderId}";

    public int? Unprotect(string? token) =>
        token is { Length: > 3 } && token.StartsWith("tok") && int.TryParse(token[3..], out var id) ? id : null;
}

internal sealed class InMemoryEventTickets : IEventTickets
{
    public List<EventTicket> Stored { get; } = [];

    public Task<IReadOnlyList<EventTicket>> GetByEventAsync(int eventId) =>
        Task.FromResult<IReadOnlyList<EventTicket>>(Stored.Where(t => t.EventId == eventId).ToList());

    public Task<IReadOnlyList<EventTicket>> GetByOrderAsync(int orderId) =>
        Task.FromResult<IReadOnlyList<EventTicket>>(Stored.Where(t => t.OrderId == orderId).ToList());

    public Task<EventTicket> SaveAsync(EventTicket ticket)
    {
        Stored.Add(ticket);
        return Task.FromResult(ticket);
    }

    public Task SetHolderAsync(Guid uuid, CardHolder holder) => Task.CompletedTask;
}

internal sealed class InMemorySeasonPasses : ISeasonPasses
{
    public List<SeasonPass> Stored { get; } = [];

    public Task<SeasonPass?> GetByUuidAsync(Guid uuid) => Task.FromResult(Stored.FirstOrDefault(p => p.Uuid == uuid));

    public Task<IReadOnlyList<SeasonPass>> GetByOrderAsync(int orderId) =>
        Task.FromResult<IReadOnlyList<SeasonPass>>(Stored.Where(p => p.OrderId == orderId).ToList());

    public Task<SeasonPass> SaveAsync(SeasonPass pass)
    {
        Stored.Add(pass);
        return Task.FromResult(pass);
    }

    public Task SetHolderAsync(Guid uuid, CardHolder holder) => Task.CompletedTask;

    public Task<(int Created, int Updated)> ImportUnifiedAsync(int seasonId, IReadOnlyList<TicketImportRow> rows, string defaultBundle,
        int? defaultTierId = null, string? createdByName = null, string? createdByEmail = null) => throw new NotSupportedException();
}

internal sealed class StubConvertibleCards : IConvertibleCards
{
    public List<(Guid FlexUuid, int EventId)> Converted { get; } = [];

    public Task<ConversionResolution> ResolveAsync(int eventId, string cardNumber, int? chosenTierId = null) => throw new NotSupportedException();

    public Task MarkFlexConvertedAsync(Guid flexUuid, int eventId)
    {
        Converted.Add((flexUuid, eventId));
        return Task.CompletedTask;
    }
}

internal sealed class RecordingOrderAddOns : IOrderAddOns
{
    public Dictionary<int, IReadOnlyList<OrderAddOnLine>> Saved { get; } = new();

    public Task SaveAsync(int orderId, IReadOnlyList<OrderAddOnLine> lines)
    {
        Saved[orderId] = lines;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OrderAddOnLine>> GetByOrderAsync(int orderId) =>
        Task.FromResult(Saved.TryGetValue(orderId, out var lines) ? lines : []);
}

internal sealed class RecordingAddOnNotifier : IAddOnNotifier
{
    public int Notifications { get; private set; }

    public Task NotifyAsync(string orderNumber, string buyerName, string buyerEmail, IReadOnlyList<OrderAddOnLine> lines, CancellationToken cancellationToken = default)
    {
        Notifications++;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingOrderMailer : IOrderMailer
{
    public List<OrderMailModel> Sent { get; } = [];

    public Task<bool> SendTicketsAsync(OrderMailModel model, CancellationToken cancellationToken = default)
    {
        Sent.Add(model);
        return Task.FromResult(true);
    }

    public Task<string> RenderAsync(OrderMailModel model) => Task.FromResult("");
}

internal sealed class RecordingOrderItems : IOrderItems
{
    public Dictionary<int, IReadOnlyList<OrderItem>> Saved { get; } = new();

    public Task SaveAsync(int orderId, IReadOnlyList<OrderItem> items)
    {
        Saved[orderId] = items;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OrderItem>> GetByOrderAsync(int orderId) =>
        Task.FromResult(Saved.TryGetValue(orderId, out var items) ? items : []);
}

internal sealed class RecordingNewsletter : INewsletterSignups
{
    public List<(string Email, string? Name, string Source)> Subscriptions { get; } = [];

    public Task SubscribeAsync(string email, string? name, string source)
    {
        Subscriptions.Add((email, name, source));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NewsletterSignup>> GetAllAsync() => Task.FromResult<IReadOnlyList<NewsletterSignup>>([]);

    public Task<IReadOnlyList<NewsletterSignup>> GetPendingAsync() => Task.FromResult<IReadOnlyList<NewsletterSignup>>([]);

    public Task SetTransferStatusAsync(int id, NewsletterTransferStatus status) => Task.CompletedTask;

    public Task MarkTransferredAsync(IEnumerable<int> ids) => Task.CompletedTask;
}

internal sealed class EmptyIssuedTickets : IIssuedTicketReader
{
    public Task<IssuedTicket?> FindAsync(Guid uuid) => Task.FromResult<IssuedTicket?>(null);

    public Task<IssuedTicket?> FindByCodeAsync(string codePrefix) => Task.FromResult<IssuedTicket?>(null);
}
