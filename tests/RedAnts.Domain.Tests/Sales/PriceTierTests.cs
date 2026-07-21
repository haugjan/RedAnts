using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using Xunit;

namespace RedAnts.Domain.Tests.Sales;

public class PriceTierTests
{
    // --- Create validation ---

    [Fact]
    public void Create_ZeroSeasonId_Throws()
    {
        Assert.Throws<DomainException>(() => PriceTier.Create(0, "Erwachsen", null, null, 0));
    }

    [Fact]
    public void Create_NegativeSeasonId_Throws()
    {
        Assert.Throws<DomainException>(() => PriceTier.Create(-1, "Erwachsen", null, null, 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankName_Throws(string name)
    {
        Assert.Throws<DomainException>(() => PriceTier.Create(1, name, null, null, 0));
    }

    [Fact]
    public void Create_NullName_Throws()
    {
        Assert.Throws<DomainException>(() => PriceTier.Create(1, null!, null, null, 0));
    }

    [Fact]
    public void Create_NameExceeds200Chars_Throws()
    {
        Assert.Throws<DomainException>(() => PriceTier.Create(1, new string('x', 201), null, null, 0));
    }

    [Fact]
    public void Create_NameExactly200Chars_Succeeds()
    {
        var tier = PriceTier.Create(1, new string('x', 200), null, null, 0);
        Assert.Equal(200, tier.Name.Length);
    }

    [Fact]
    public void Create_NegativeMaxAge_Throws()
    {
        Assert.Throws<DomainException>(() => PriceTier.Create(1, "Jugend", maxAge: -1, null, 0));
    }

    [Fact]
    public void Create_ZeroMaxAge_Succeeds()
    {
        var tier = PriceTier.Create(1, "Kind", maxAge: 0, null, 0);
        Assert.Equal(0, tier.MaxAge);
    }

    [Fact]
    public void Create_NullMaxAge_Succeeds()
    {
        var tier = PriceTier.Create(1, "Erwachsen", maxAge: null, null, 0);
        Assert.Null(tier.MaxAge);
    }

    // --- Create normalisation ---

    [Fact]
    public void Create_TrimsName()
    {
        var tier = PriceTier.Create(1, "  Erwachsen  ", null, null, 0);
        Assert.Equal("Erwachsen", tier.Name);
    }

    [Fact]
    public void Create_StartsWithIdZero()
    {
        var tier = PriceTier.Create(1, "Erwachsen", null, null, 0);
        Assert.Equal(0, tier.Id);
    }

    // --- Create stores values ---

    [Fact]
    public void Create_SetsAllProperties()
    {
        var tier = PriceTier.Create(seasonId: 7, "Jugend", maxAge: 19, promoOfTierId: 2, sortOrder: 3);

        Assert.Equal(7, tier.SeasonId);
        Assert.Equal("Jugend", tier.Name);
        Assert.Equal(19, tier.MaxAge);
        Assert.Equal(2, tier.PromoOfTierId);
        Assert.Equal(3, tier.SortOrder);
    }

    // --- IsPromo ---

    [Fact]
    public void IsPromo_NullPromoOfTierId_ReturnsFalse()
    {
        var tier = PriceTier.Create(1, "Erwachsen", null, promoOfTierId: null, 0);
        Assert.False(tier.IsPromo);
    }

    [Fact]
    public void IsPromo_PromoOfTierIdSet_ReturnsTrue()
    {
        var tier = PriceTier.Create(1, "Sonderaktion", null, promoOfTierId: 5, 0);
        Assert.True(tier.IsPromo);
    }

    // --- FromPersistence ---

    [Fact]
    public void FromPersistence_SetsId()
    {
        var tier = PriceTier.FromPersistence(42, 1, "Erwachsen", null, null, 0);
        Assert.Equal(42, tier.Id);
    }

    [Fact]
    public void FromPersistence_SetsAllProperties()
    {
        var tier = PriceTier.FromPersistence(id: 10, seasonId: 3, "Jugend", maxAge: 19, promoOfTierId: 1, sortOrder: 2);

        Assert.Equal(10, tier.Id);
        Assert.Equal(3, tier.SeasonId);
        Assert.Equal("Jugend", tier.Name);
        Assert.Equal(19, tier.MaxAge);
        Assert.Equal(1, tier.PromoOfTierId);
        Assert.Equal(2, tier.SortOrder);
    }
}
