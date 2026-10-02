using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Checkout;
using Xunit;

namespace RedAnts.Ticketing.Tests.Checkout;

public class GetQuickBuyCartTests
{
    private const int EventId = 7;

    private static GetQuickBuyCart.Handler Handler(QuickBuyPricing pricing, bool conversionOnly = false) =>
        new(pricing, new QuickBuyEvents(), new QuickBuyConversionRules(conversionOnly));

    private static QuickBuyPricing Pricing() => new()
    {
        Categories =
        {
            new AvailableTicketCategory(1, "Erwachsene", 20m, true, 100),
            new AvailableTicketCategory(2, "Jugendliche", 5m, true, 100),
            new AvailableTicketCategory(3, "Kinder", 0m, true, 100)
        }
    };

    private static GetQuickBuyCart.Query Query(params (int TierId, int Quantity)[] lines) =>
        new(EventId, lines.Select(l => new GetQuickBuyCart.Line(l.TierId, l.Quantity)).ToList());

    [Fact]
    public async Task Mixed_categories_land_in_one_cart()
    {
        var cart = await Handler(Pricing()).HandleAsync(Query((1, 2), (2, 1), (3, 3)));

        Assert.NotNull(cart);
        Assert.Equal(6, cart.TotalQuantity);
        Assert.Equal(45m, cart.TotalAmount.Amount);
        Assert.Equal(3, cart.Items.Count);
    }

    [Fact]
    public async Task Categories_without_quantity_are_skipped()
    {
        var cart = await Handler(Pricing()).HandleAsync(Query((1, 2), (2, 0), (3, -1)));

        Assert.NotNull(cart);
        var line = Assert.Single(cart.Items);
        Assert.Equal(1, line.TierId);
        Assert.Equal(2, line.Quantity);
    }

    [Fact]
    public async Task Quantity_is_capped_per_category()
    {
        var cart = await Handler(Pricing()).HandleAsync(Query((1, 40)));

        Assert.NotNull(cart);
        Assert.Equal(GetQuickBuyCart.MaxPerCategory, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public async Task Repeated_tier_is_merged_before_the_cap_applies()
    {
        var cart = await Handler(Pricing()).HandleAsync(Query((1, 6), (1, 6)));

        Assert.NotNull(cart);
        Assert.Equal(GetQuickBuyCart.MaxPerCategory, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public async Task Quantity_never_exceeds_the_remaining_tickets()
    {
        var pricing = new QuickBuyPricing
        {
            Categories = { new AvailableTicketCategory(1, "Erwachsene", 20m, true, 2) }
        };

        var cart = await Handler(pricing).HandleAsync(Query((1, 5)));

        Assert.NotNull(cart);
        Assert.Equal(2, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public async Task Sold_out_categories_are_left_out()
    {
        var pricing = new QuickBuyPricing
        {
            Categories =
            {
                new AvailableTicketCategory(1, "Erwachsene", 20m, false, 0),
                new AvailableTicketCategory(2, "Jugendliche", 5m, true, 100)
            }
        };

        var cart = await Handler(pricing).HandleAsync(Query((1, 2), (2, 1)));

        Assert.NotNull(cart);
        Assert.Equal(2, Assert.Single(cart.Items).TierId);
    }

    [Fact]
    public async Task Nothing_buyable_yields_no_cart()
    {
        var cart = await Handler(Pricing()).HandleAsync(Query((1, 0), (99, 3)));

        Assert.Null(cart);
    }

    [Fact]
    public async Task Conversion_only_event_yields_no_cart()
    {
        var cart = await Handler(Pricing(), conversionOnly: true).HandleAsync(Query((1, 1)));

        Assert.Null(cart);
    }
}

internal sealed class QuickBuyPricing : IEventPricing
{
    public List<AvailableTicketCategory> Categories { get; } = [];

    public Task<IReadOnlyList<AvailableTicketCategory>> GetAvailableAsync(int eventId) =>
        Task.FromResult<IReadOnlyList<AvailableTicketCategory>>(Categories);

    public Task<AvailableTicketCategory?> FindAvailableByTierAsync(int eventId, int tierId) =>
        Task.FromResult(Categories.FirstOrDefault(c => c.TierId == tierId));

    public Task<string?> CheckCapacityAsync(IReadOnlyList<TicketDemand> demand) => Task.FromResult<string?>(null);
}

internal sealed class QuickBuyEvents : IEventReader
{
    private static readonly Event Match = Event.FromPersistence(7, "Red Ants vs. Gegner", null, 1,
        new DateOnly(2026, 10, 3), new TimeOnly(18, 0), false, 1, EventStatus.Open, null, null, null, null);

    public Task<IReadOnlyList<Event>> GetAllAsync() => Task.FromResult<IReadOnlyList<Event>>([Match]);

    public Task<IReadOnlyList<Event>> GetPublicOpenAsync() => Task.FromResult<IReadOnlyList<Event>>([Match]);

    public Task<IReadOnlyList<Event>> GetUpcomingForScanningAsync() => Task.FromResult<IReadOnlyList<Event>>([Match]);

    public Task<IReadOnlyList<Event>> GetBySeasonAsync(int seasonId) => Task.FromResult<IReadOnlyList<Event>>([Match]);

    public Task<Event?> FindByIdAsync(int id) => Task.FromResult(id == Match.Id ? Match : null);
}

internal sealed class QuickBuyConversionRules(bool conversionOnly) : IEventConversionRuleReader
{
    public Task<IReadOnlyList<EventConversionRule>> GetByEventAsync(int eventId) =>
        Task.FromResult<IReadOnlyList<EventConversionRule>>([]);

    public Task<bool> GetConversionOnlyAsync(int eventId) => Task.FromResult(conversionOnly);
}
