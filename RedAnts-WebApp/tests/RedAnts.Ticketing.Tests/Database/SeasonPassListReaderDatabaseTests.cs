using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog.Infrastructure;
using RedAnts.Ticketing.Features.Orders.Infrastructure;
using RedAnts.Ticketing.Features.SeasonPasses.Infrastructure;
using RedAnts.Ticketing.Tests.Checkout;
using RedAnts.Ticketing.Tests.Tickets;
using Xunit;
using PaymentMethod = RedAnts.Ticketing.Domain.Sales.PaymentMethod;

namespace RedAnts.Ticketing.Tests.Database;

[Collection(AgentDatabaseCollection.Name)]
public class SeasonPassListReaderDatabaseTests : IAsyncLifetime
{
    private readonly AgentDatabaseFixture _agent = new();

    public Task InitializeAsync() => _agent.InitializeAsync();

    public Task DisposeAsync() => _agent.DisposeAsync();

    private SeasonPassRepository Passes => new(_agent.Scopes, new PriceTierRepository(_agent.Scopes));

    private SeasonPassListReader Reader => new(_agent.Scopes, new StubTicketTokens(), new StubPublicBaseUrl());

    private static FakeTimeProvider At(int hour) => new(new DateTimeOffset(2026, 7, 15, hour, 0, 0, TimeSpan.Zero));

    [DatabaseFact]
    public async Task The_passes_of_the_season_are_listed_newest_first()
    {
        var seasonId = _agent.UnusedId;
        var buyer = Buyer.Create(BuyerType.Private, "Anna", "Muster", null);

        var older = await Passes.SaveAsync(SeasonPass.Create(seasonId, null, 220m, null, buyer,
            "Tester", "tester@example.ch", "Reihe A", "anna@example.ch", At(8)));
        var newer = await Passes.SaveAsync(SeasonPass.Create(seasonId, null, 120m, null, null, "Tester", time: At(9)));
        await Passes.SaveAsync(SeasonPass.Create(seasonId + 1, null, 99m, null));

        var rows = await Reader.GetBySeasonAsync(seasonId);

        Assert.Equal(2, rows.Count);
        Assert.Equal([newer.Uuid, older.Uuid], rows.Select(r => r.Uuid).ToArray());

        var listed = rows.Single(r => r.Uuid == older.Uuid);

        Assert.Equal(220m, listed.Price);
        Assert.Equal(TicketStatus.Valid, listed.Status);
        Assert.Equal(new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.FromHours(2)), listed.CreatedAt);
        Assert.Equal("Anna Muster", listed.BuyerName);
        Assert.Equal("anna@example.ch", listed.Email);
        Assert.Equal("Reihe A", listed.Reference);
        Assert.Equal("Tester", listed.CreatedByName);
        Assert.Equal("–", listed.CategoryName);
        Assert.Equal(0, listed.EventVisits);
        Assert.Null(listed.OrderNumber);
        Assert.Equal($"https://tickets.test/ticket/short-{older.Uuid:N}"[..42], listed.TicketUrl);
    }

    [DatabaseFact]
    public async Task A_pass_shows_the_payment_state_of_its_order()
    {
        var seasonId = _agent.UnusedId;
        var orders = new OrderRepository(_agent.Scopes, new ConfigurationBuilder().Build());
        var address = BillingAddress.Create(BuyerType.Private, "Anna", "Muster", null,
            "Bahnhofstrasse 1", null, "8400", "Winterthur", "Schweiz", "anna@example.ch", null);
        var order = await orders.SaveAsync(Order.Create(
            await orders.NextOrderNumberAsync(), address, 220m, 0.081m, PaymentMethod.Twint, null));

        Assert.True(await orders.TryMarkPaidAsync(order.Id));

        var pass = await Passes.SaveAsync(SeasonPass.Create(seasonId, null, 220m, order.Id));

        var row = Assert.Single(await Reader.GetBySeasonAsync(seasonId));

        Assert.Equal(pass.Uuid, row.Uuid);
        Assert.Equal(order.OrderNumber, row.OrderNumber);
        Assert.Equal("bezahlt", row.PaymentState);
    }
}
