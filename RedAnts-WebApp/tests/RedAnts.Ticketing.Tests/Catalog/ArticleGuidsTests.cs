using RedAnts.Ticketing.Features.Catalog.Infrastructure;
using Xunit;

namespace RedAnts.Ticketing.Tests.Catalog;

public class ArticleGuidsTests
{
    private sealed record TierRow(int? TierId, int Category, Guid? ArticleGuid);

    private sealed record AddOnRow(int Id, string Label, Guid? ArticleGuid);

    [Fact]
    public void Same_tier_and_category_keeps_its_guid()
    {
        var guid = Guid.NewGuid();
        var articles = ArticleGuids.ByTierAndCategory([new TierRow(3, 1, guid)], r => r.TierId, r => r.Category, r => r.ArticleGuid);

        Assert.Equal(guid, articles.Keep(3, 1));
        Assert.NotEqual(guid, articles.Keep(3, 2));
        Assert.NotEqual(guid, articles.Keep(null, 1));
    }

    [Fact]
    public void Rows_without_guid_get_a_fresh_one()
    {
        var articles = ArticleGuids.ByTierAndCategory([new TierRow(null, 1, null)], r => r.TierId, r => r.Category, r => r.ArticleGuid);

        Assert.NotEqual(Guid.Empty, articles.Keep(null, 1));
    }

    [Fact]
    public void Add_ons_match_by_id_first_then_by_label()
    {
        var byId = Guid.NewGuid();
        var byLabel = Guid.NewGuid();
        var articles = ArticleGuids.ByIdOrLabel(
            [new AddOnRow(5, "Parkplatz", byId), new AddOnRow(6, "Garderobe", byLabel)],
            r => r.Id, r => r.Label, r => r.ArticleGuid);

        Assert.Equal(byId, articles.Keep(5, "Etwas anderes"));
        Assert.Equal(byLabel, articles.Keep(0, "  garderobe "));
        Assert.NotEqual(byId, articles.Keep(0, "Neu"));
    }
}
