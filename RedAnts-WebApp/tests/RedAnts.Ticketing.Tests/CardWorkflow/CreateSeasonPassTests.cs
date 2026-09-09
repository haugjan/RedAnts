using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.CardWorkflow;
using Xunit;

namespace RedAnts.Ticketing.Tests.CardWorkflow;

public class CreateSeasonPassTests
{
    [Fact]
    public async Task Saves_the_pass_with_order_buyer_and_holder()
    {
        var passes = new InMemorySeasonPasses();
        var buyer = Buyer.Create(BuyerType.Private, "Anna", "Muster", null);
        var holder = CardHolder.Create(BuyerType.Private, null, null, "Anna", "Muster", null, "anna@example.ch", null, null, null, null, null, null);

        var saved = await new CreateSeasonPass.Handler(passes).HandleAsync(
            new CreateSeasonPass.Command(3, 7, 300m, 42, buyer, " Bundle A ", "anna@example.ch", holder, "Admin", "admin@redants.ch"));

        Assert.Equal(3, saved.SeasonId);
        Assert.Equal(7, saved.TierId);
        Assert.Equal(300m, saved.Price);
        Assert.Equal(42, saved.OrderId);
        Assert.Equal("Bundle A", saved.Reference);
        Assert.Equal("anna@example.ch", saved.Email);
        Assert.Single(passes.Saved);
        Assert.Contains(passes.Holders, h => h.Uuid == saved.Uuid && h.Holder == holder);
    }

    [Fact]
    public async Task Rejects_a_negative_price_before_saving()
    {
        var passes = new InMemorySeasonPasses();
        var buyer = Buyer.Create(BuyerType.Private, "Anna", "Muster", null);
        var holder = CardHolder.Create(BuyerType.Private, null, null, "Anna", "Muster", null, null, null, null, null, null, null, null);

        await Assert.ThrowsAsync<DomainException>(() => new CreateSeasonPass.Handler(passes).HandleAsync(
            new CreateSeasonPass.Command(3, 7, -1m, 42, buyer, null, null, holder, null, null)));

        Assert.Empty(passes.Saved);
    }
}
