using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Checkout;
using RedAnts.Ticketing.Features.MemberCards;
using Xunit;

namespace RedAnts.Ticketing.Tests.Checkout;

public class CanConvertTests
{
    private static readonly Guid CardUuid = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private readonly CheckoutFixture _fixture = new();

    private static ConversionOffer Offer(int remainingCap = 1, int? eventRemaining = null) => new(
        TicketType.MemberCard, CardUuid, CheckoutFixture.SeasonId, (int)TicketCategory.Adult, CheckoutFixture.AdultTier,
        0m, "Mitgliederkarte ABCD1234", remainingCap, "Red Ants vs. Gegner", eventRemaining);

    private Task<CheckResult> CheckAsync() =>
        _fixture.CanConvert.HandleAsync(new CanConvert.Check(CheckoutFixture.EventId, "ABCD1234"));

    private void Convert(int cap = 1) =>
        _fixture.Carts.Load().AddConversion(CheckoutFixture.EventId, "Red Ants vs. Gegner", CheckoutFixture.SeasonId,
            CheckoutFixture.AdultTier, "Mitgliederkarte ABCD1234", Money.Of(0m),
            new ConversionOrigin(TicketType.MemberCard, CardUuid, "Mitgliederkarte ABCD1234", (int)TicketCategory.Adult, cap));

    [Fact]
    public async Task A_resolvable_card_may_be_converted()
    {
        _fixture.ConvertibleCards.Resolution = new ConversionResolution(true, null, Offer());

        Assert.True((await CheckAsync()).IsAllowed);
        Assert.Equal((CheckoutFixture.EventId, "ABCD1234", null), _fixture.ConvertibleCards.Resolutions.Single());
    }

    [Fact]
    public async Task An_unknown_card_is_denied_with_the_resolver_reason()
    {
        _fixture.ConvertibleCards.Resolution = new ConversionResolution(false, "Keine passende Karte mit dieser Nummer für diesen Anlass gefunden.", null);

        var denied = Assert.IsType<CheckResult.Denied>(await CheckAsync());

        Assert.IsType<CanConvert.CardNotUsable>(denied.Cause);
        Assert.Equal("Keine passende Karte mit dieser Nummer für diesen Anlass gefunden.", denied.Cause.Message);
    }

    [Fact]
    public async Task A_missing_tier_choice_is_denied_with_the_choices()
    {
        _fixture.ConvertibleCards.Resolution = new ConversionResolution(false, "Bitte Kategorie für die Umwandlung wählen.", null,
            [new ConversionTierChoice(CheckoutFixture.AdultTier, "Erwachsen", 15m)]);

        var denied = Assert.IsType<CheckResult.Denied>(await CheckAsync());

        var tierChoice = Assert.IsType<CanConvert.TierChoiceRequired>(denied.Cause);
        Assert.Equal("Bitte Kategorie für die Umwandlung wählen.", tierChoice.Message);
        Assert.Equal(CheckoutFixture.AdultTier, Assert.Single(tierChoice.Choices).TierId);
    }

    [Fact]
    public async Task A_sold_out_event_is_denied()
    {
        _fixture.ConvertibleCards.Resolution = new ConversionResolution(true, null, Offer(eventRemaining: 1));
        Convert();

        var denied = Assert.IsType<CheckResult.Denied>(await CheckAsync());

        Assert.IsType<ConversionDenied.EventSoldOut>(denied.Cause);
        Assert.Contains("Kontingent ausgeschöpft", denied.Cause.Message);
    }

    [Fact]
    public async Task A_card_that_is_already_in_the_cart_is_denied()
    {
        _fixture.ConvertibleCards.Resolution = new ConversionResolution(true, null, Offer());
        Convert();

        var denied = Assert.IsType<CheckResult.Denied>(await CheckAsync());

        Assert.IsType<ConversionDenied.CardExhausted>(denied.Cause);
        Assert.Equal("Für diese Karte sind bereits alle Umwandlungen im Warenkorb.", denied.Cause.Message);
    }

    [Fact]
    public async Task A_card_with_two_admissions_may_be_converted_twice()
    {
        _fixture.ConvertibleCards.Resolution = new ConversionResolution(true, null, Offer(remainingCap: 2));
        Convert(cap: 2);

        Assert.True((await CheckAsync()).IsAllowed);
    }

    [Fact]
    public async Task The_command_denies_what_the_check_denies()
    {
        _fixture.ConvertibleCards.Resolution = new ConversionResolution(true, null, Offer());
        Convert();

        var result = await _fixture.AddConversion.HandleAsync(new AddConversionToCart.Command(CheckoutFixture.EventId, "ABCD1234", null));

        Assert.False(result.Added);
        Assert.Equal(new ConversionDenied.CardExhausted().Message, result.Message);
        Assert.Equal(1, result.Cart.TotalQuantity);
    }

    [Fact]
    public async Task The_command_adds_the_converted_ticket()
    {
        _fixture.ConvertibleCards.Resolution = new ConversionResolution(true, null, Offer());

        var result = await _fixture.AddConversion.HandleAsync(new AddConversionToCart.Command(CheckoutFixture.EventId, "ABCD1234", null));

        Assert.True(result.Added);
        Assert.Contains("Umgewandeltes Ticket", result.Message);
        var line = Assert.Single(_fixture.Carts.Load().Items);
        Assert.True(line.IsConversion);
        Assert.Equal(CardUuid, line.Origin!.CardUuid);
    }
}
