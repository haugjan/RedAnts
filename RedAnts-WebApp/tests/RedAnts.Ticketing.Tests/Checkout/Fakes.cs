using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admission;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Checkout;
using RedAnts.Ticketing.Features.Email;
using RedAnts.Ticketing.Features.MemberCards;
using RedAnts.Ticketing.Features.Newsletter;
using RedAnts.Ticketing.Features.Orders;
using RedAnts.Ticketing.Features.SeasonPasses;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Tests.Checkout;

internal sealed class InMemoryCart : ICartRepository
{
    private Cart _cart = Cart.Empty();

    public Cart Load() => _cart;

    public void Save(Cart cart) => _cart = cart;

    public void Clear() => _cart = Cart.Empty();
}

internal sealed class InMemoryOrderRepository : IOrderRepository
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

    public Task CopyBillingToTicketsAsync(int orderId)
    {
        BillingCopies++;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryDraftOrdersReader(InMemoryOrderRepository orders) : IDraftOrdersReader
{
    public Task<IReadOnlyList<int>> GetIdsCreatedBetweenAsync(DateTimeOffset createdAfter, DateTimeOffset createdBefore) =>
        Task.FromResult<IReadOnlyList<int>>(orders.Stored
            .Where(o => o.Status == OrderStatus.Draft && o.CreatedAt >= createdAfter && o.CreatedAt < createdBefore)
            .Select(o => o.Id)
            .ToList());
}

internal sealed class InMemoryEventPrices : IEventPriceRepository
{
    private readonly Dictionary<int, EventPrice> _prices = new();

    public int FailingSaves { get; set; }
    public int Saves { get; private set; }

    public void Seed(EventPrice price) => _prices[price.EventId] = price;

    public EventPrice Stored(int eventId) => _prices[eventId];

    public Task<EventPrice?> GetByEventAsync(int eventId) =>
        Task.FromResult(_prices.TryGetValue(eventId, out var price) ? Copy(price, price.Version) : null);

    public Task<EventPrice> SaveAsync(EventPrice price) => throw new NotSupportedException();

    public Task DeleteAsync(int eventPriceId) => throw new NotSupportedException();

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

internal sealed class InMemorySeasonPrices : ISeasonPriceRepository
{
    private readonly Dictionary<int, SeasonPrice> _prices = new();

    public int FailingSaves { get; set; }

    public void Seed(SeasonPrice price) => _prices[price.SeasonId] = price;

    public SeasonPrice Stored(int seasonId) => _prices[seasonId];

    public Task<SeasonPrice?> GetBySeasonAsync(int seasonId) =>
        Task.FromResult(_prices.TryGetValue(seasonId, out var price) ? Copy(price, price.Version) : null);

    public Task<SeasonPrice> SaveAsync(SeasonPrice price) => throw new NotSupportedException();

    public Task DeleteAsync(int seasonPriceId) => throw new NotSupportedException();

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

internal sealed class StubCapacityUsage : ICapacityUsageReader
{
    public Dictionary<int, CapacityUsage> EventUsage { get; } = new();
    public Dictionary<int, CapacityUsage> PassUsage { get; } = new();

    public Task<CapacityUsage> GetEventUsageAsync(int eventId) =>
        Task.FromResult(EventUsage.TryGetValue(eventId, out var usage) ? usage : CapacityUsage.None);

    public Task<CapacityUsage> GetSeasonPassUsageAsync(int seasonId) =>
        Task.FromResult(PassUsage.TryGetValue(seasonId, out var usage) ? usage : CapacityUsage.None);
}

internal sealed class RecordingOrderLog : IOrderLog
{
    public List<(int OrderId, OrderStatus Status, string? By, string? Note)> Entries { get; } = [];
    public bool Throws { get; set; }

    public Task AppendAsync(int orderId, OrderStatus toStatus, string? changedBy, string? note = null)
    {
        if (Throws) throw new InvalidOperationException("order log unavailable");
        Entries.Add((orderId, toStatus, changedBy, note));
        return Task.CompletedTask;
    }
}

internal sealed class StubConversionRules : IEventConversionRuleRepository
{
    public HashSet<int> ConversionOnlyEvents { get; } = [];

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

internal sealed class StubSeasonAddOns : ISeasonAddOnRepository
{
    public List<SeasonAddOn> AddOns { get; } = [];

    public Task<SeasonAddOnSet> LoadSeasonAsync(int seasonId) =>
        Task.FromResult(new SeasonAddOnSet(seasonId, AddOns.Where(a => a.SeasonId == seasonId).ToList()));

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

internal sealed class InMemoryEventTicketRepository : IEventTicketRepository
{
    public List<EventTicket> Stored { get; } = [];
    public List<(Guid Uuid, CardHolder Holder)> Holders { get; } = [];

    public Task<EventTicket?> GetByUuidAsync(Guid uuid) => Task.FromResult(Stored.FirstOrDefault(t => t.Uuid == uuid));

    public int FailingSaveNumber { get; set; }

    public Task<EventTicket> SaveAsync(EventTicket ticket)
    {
        if (FailingSaveNumber > 0 && Stored.Count + 1 == FailingSaveNumber)
            throw new InvalidOperationException("ticket table unavailable");
        Stored.RemoveAll(t => t.Uuid == ticket.Uuid);
        Stored.Add(ticket);
        return Task.FromResult(ticket);
    }

    public Task SetHolderAsync(Guid uuid, CardHolder holder)
    {
        Holders.Add((uuid, holder));
        return Task.CompletedTask;
    }
}

internal sealed class InMemorySeasonPasses : ISeasonPassRepository
{
    public List<SeasonPass> Stored { get; } = [];

    public Task<SeasonPass?> GetByUuidAsync(Guid uuid) => Task.FromResult(Stored.FirstOrDefault(p => p.Uuid == uuid));

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
    public List<(int EventId, string CardNumber, int? TierId)> Resolutions { get; } = [];
    public ConversionResolution? Resolution { get; set; }

    public Task<ConversionResolution> ResolveAsync(int eventId, string cardNumber, int? chosenTierId = null)
    {
        Resolutions.Add((eventId, cardNumber, chosenTierId));
        return Task.FromResult(Resolution ?? throw new NotSupportedException());
    }

    public Task MarkFlexConvertedAsync(Guid flexUuid, int eventId)
    {
        Converted.Add((flexUuid, eventId));
        return Task.CompletedTask;
    }
}

internal sealed class RecordingOrderAddOns : IOrderAddOns
{
    public Dictionary<int, IReadOnlyList<OrderAddOnLine>> Saved { get; } = new();
    public List<(int OrderAddOnId, bool Delivered)> Deliveries { get; } = [];

    public Task SaveAsync(int orderId, IReadOnlyList<OrderAddOnLine> lines)
    {
        Saved[orderId] = lines;
        return Task.CompletedTask;
    }

    public Task SetDeliveredAsync(int orderAddOnId, bool delivered)
    {
        Deliveries.Add((orderAddOnId, delivered));
        return Task.CompletedTask;
    }
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
}

internal sealed class RecordingNewsletterSignupRepository : INewsletterSignupRepository
{
    public List<(string Email, string? Name, string Source)> Subscriptions { get; } = [];
    public List<(int Id, NewsletterTransferStatus Status)> StatusChanges { get; } = [];
    public List<int> MarkedTransferred { get; } = [];

    public Task SubscribeAsync(string email, string? name, string source)
    {
        Subscriptions.Add((email, name, source));
        return Task.CompletedTask;
    }

    public Task SetTransferStatusAsync(int id, NewsletterTransferStatus status)
    {
        StatusChanges.Add((id, status));
        return Task.CompletedTask;
    }

    public Task MarkTransferredAsync(IEnumerable<int> ids)
    {
        MarkedTransferred.AddRange(ids);
        return Task.CompletedTask;
    }
}

internal sealed class EmptyIssuedTickets : IIssuedTicketReader
{
    public Task<IssuedTicket?> FindAsync(Guid uuid) => Task.FromResult<IssuedTicket?>(null);

    public Task<IssuedTicket?> FindByCodeAsync(string codePrefix) => Task.FromResult<IssuedTicket?>(null);
}

internal sealed class RecordingUnitOfWork : IUnitOfWork
{
    public int Started { get; private set; }
    public int Committed { get; private set; }
    public int RolledBack { get; private set; }

    public async Task<T> RunAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken = default)
    {
        Started++;
        try
        {
            var result = await work();
            Committed++;
            return result;
        }
        catch
        {
            RolledBack++;
            throw;
        }
    }

    public async Task RunAsync(Func<Task> work, CancellationToken cancellationToken = default) =>
        await RunAsync<object?>(async () =>
        {
            await work();
            return null;
        }, cancellationToken);
}
