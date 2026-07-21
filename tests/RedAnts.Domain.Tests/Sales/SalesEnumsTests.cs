using RedAnts.Domain.Ticketing.Sales;
using Xunit;

namespace RedAnts.Domain.Tests.Sales;

public class SalesEnumsTests
{
    // --- AddOnScope.DisplayName ---

    [Theory]
    [InlineData(AddOnScope.PerPass, "pro Saisonkarte")]
    [InlineData(AddOnScope.PerOrder, "einmalig pro Bestellung")]
    public void AddOnScope_DisplayName_ReturnsGermanLabel(AddOnScope scope, string expected)
    {
        Assert.Equal(expected, scope.DisplayName());
    }

    // --- PaymentSource.DisplayName ---

    [Theory]
    [InlineData(PaymentSource.Sponsoring, "Sponsoring")]
    [InlineData(PaymentSource.Marketing, "Marketing")]
    [InlineData(PaymentSource.Goodwill, "Goodwill")]
    [InlineData(PaymentSource.Online, "Online")]
    [InlineData(PaymentSource.Cash, "Cash")]
    [InlineData(PaymentSource.TwintCode, "TWINT-Code")]
    [InlineData(PaymentSource.Terminal, "Terminal")]
    [InlineData(PaymentSource.Invoice, "Rechnung")]
    public void PaymentSource_DisplayName_ReturnsGermanLabel(PaymentSource source, string expected)
    {
        Assert.Equal(expected, source.DisplayName());
    }

    // --- PaymentSource.AdminChoicesForPrice ---

    [Fact]
    public void AdminChoicesForPrice_PositiveAmount_ReturnsCashChannels()
    {
        var choices = PaymentSourceExtensions.AdminChoicesForPrice(0.01m);
        Assert.Equal(
            [PaymentSource.Cash, PaymentSource.TwintCode, PaymentSource.Terminal, PaymentSource.Invoice],
            choices);
    }

    [Fact]
    public void AdminChoicesForPrice_LargePositiveAmount_ReturnsCashChannels()
    {
        var choices = PaymentSourceExtensions.AdminChoicesForPrice(1000m);
        Assert.Equal(
            [PaymentSource.Cash, PaymentSource.TwintCode, PaymentSource.Terminal, PaymentSource.Invoice],
            choices);
    }

    [Fact]
    public void AdminChoicesForPrice_ZeroAmount_ReturnsFreeSourcesOnly()
    {
        var choices = PaymentSourceExtensions.AdminChoicesForPrice(0m);
        Assert.Equal(
            [PaymentSource.Sponsoring, PaymentSource.Marketing, PaymentSource.Goodwill],
            choices);
    }

    [Fact]
    public void AdminChoicesForPrice_NegativeAmount_ReturnsFreeSourcesOnly()
    {
        var choices = PaymentSourceExtensions.AdminChoicesForPrice(-1m);
        Assert.Equal(
            [PaymentSource.Sponsoring, PaymentSource.Marketing, PaymentSource.Goodwill],
            choices);
    }

    [Fact]
    public void AdminChoicesForPrice_PositiveAmount_ExcludesFreeChannels()
    {
        var choices = PaymentSourceExtensions.AdminChoicesForPrice(10m);
        Assert.DoesNotContain(PaymentSource.Sponsoring, choices);
        Assert.DoesNotContain(PaymentSource.Marketing, choices);
        Assert.DoesNotContain(PaymentSource.Goodwill, choices);
    }

    [Fact]
    public void AdminChoicesForPrice_ZeroAmount_ExcludesCashChannels()
    {
        var choices = PaymentSourceExtensions.AdminChoicesForPrice(0m);
        Assert.DoesNotContain(PaymentSource.Cash, choices);
        Assert.DoesNotContain(PaymentSource.TwintCode, choices);
        Assert.DoesNotContain(PaymentSource.Terminal, choices);
        Assert.DoesNotContain(PaymentSource.Invoice, choices);
    }

    // --- BuyerType.DisplayName ---

    [Theory]
    [InlineData(BuyerType.Private, "Privatperson")]
    [InlineData(BuyerType.Company, "Firma")]
    public void BuyerType_DisplayName_ReturnsGermanLabel(BuyerType type, string expected)
    {
        Assert.Equal(expected, type.DisplayName());
    }

    // --- FreeEntryType.DisplayName ---

    [Theory]
    [InlineData(FreeEntryType.Player, "Spieler:in")]
    [InlineData(FreeEntryType.Staff, "Staff")]
    [InlineData(FreeEntryType.Official, "Funktionär")]
    [InlineData(FreeEntryType.SwissUnihockeyFreeCard, "SU-Freieintritt")]
    [InlineData(FreeEntryType.Child, "Kind (gratis)")]
    [InlineData(FreeEntryType.Helper, "Helfer")]
    public void FreeEntryType_DisplayName_ReturnsGermanLabel(FreeEntryType type, string expected)
    {
        Assert.Equal(expected, type.DisplayName());
    }

    // --- TicketStatus.DisplayName ---

    [Theory]
    [InlineData(TicketStatus.Valid, "Gültig")]
    [InlineData(TicketStatus.Cancelled, "Storniert")]
    [InlineData(TicketStatus.Blocked, "Gesperrt")]
    public void TicketStatus_DisplayName_ReturnsGermanLabel(TicketStatus status, string expected)
    {
        Assert.Equal(expected, status.DisplayName());
    }

    // --- TicketCategory.DisplayName ---

    [Theory]
    [InlineData(TicketCategory.Adult, "Erwachsen")]
    [InlineData(TicketCategory.AdultPromo, "Sonderaktion Erwachsen")]
    [InlineData(TicketCategory.Youth, "Jugend (bis 19)")]
    [InlineData(TicketCategory.YouthPromo, "Sonderaktion Jugend")]
    [InlineData(TicketCategory.Child, "Kind (bis 5)")]
    public void TicketCategory_DisplayName_ReturnsGermanLabel(TicketCategory category, string expected)
    {
        Assert.Equal(expected, category.DisplayName());
    }

    // --- TicketCategory.IsPromo ---

    [Theory]
    [InlineData(TicketCategory.Adult, false)]
    [InlineData(TicketCategory.AdultPromo, true)]
    [InlineData(TicketCategory.Youth, false)]
    [InlineData(TicketCategory.YouthPromo, true)]
    [InlineData(TicketCategory.Child, false)]
    public void TicketCategory_IsPromo_CorrectForAllValues(TicketCategory category, bool expected)
    {
        Assert.Equal(expected, category.IsPromo());
    }

    [Fact]
    public void TicketCategory_IsPromo_OnlyPromoVariantsAreTrue()
    {
        var promoCategories = Enum.GetValues<TicketCategory>().Where(c => c.IsPromo()).ToList();
        Assert.Equal([TicketCategory.AdultPromo, TicketCategory.YouthPromo], promoCategories);
    }

    // --- TicketCategory.PromoCounterpart ---

    [Fact]
    public void PromoCounterpart_Adult_ReturnsAdultPromo()
    {
        Assert.Equal(TicketCategory.AdultPromo, TicketCategory.Adult.PromoCounterpart());
    }

    [Fact]
    public void PromoCounterpart_Youth_ReturnsYouthPromo()
    {
        Assert.Equal(TicketCategory.YouthPromo, TicketCategory.Youth.PromoCounterpart());
    }

    [Theory]
    [InlineData(TicketCategory.AdultPromo)]
    [InlineData(TicketCategory.YouthPromo)]
    [InlineData(TicketCategory.Child)]
    public void PromoCounterpart_PromoAndChild_ReturnsNull(TicketCategory category)
    {
        Assert.Null(category.PromoCounterpart());
    }

    [Fact]
    public void PromoCounterpart_BaseCategories_AllHaveCounterpartOrNull()
    {
        Assert.NotNull(TicketCategory.Adult.PromoCounterpart());
        Assert.NotNull(TicketCategory.Youth.PromoCounterpart());
        Assert.Null(TicketCategory.Child.PromoCounterpart());
    }
}
