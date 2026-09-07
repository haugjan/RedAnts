using RedAnts.Domain;
using RedAnts.Domain.Show;
using Xunit;

namespace RedAnts.Show.Tests;

public class ShowProfileRulesTests
{
    private static ShowProfile Profile(string id, string name = "Profil") => new(id, name, "#C8102E", []);

    [Fact]
    public void Distinct_profiles_pass() =>
        ShowProfileRules.Validate([Profile("a"), Profile("b")]);

    [Fact]
    public void Blank_id_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => ShowProfileRules.Validate([Profile(" ")]));
        Assert.Equal(ShowProfileRules.BlankId, ex.Message);
    }

    [Fact]
    public void Blank_name_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => ShowProfileRules.Validate([Profile("a", "")]));
        Assert.Equal(ShowProfileRules.BlankName, ex.Message);
    }

    [Fact]
    public void Duplicate_ids_are_rejected_regardless_of_case()
    {
        var ex = Assert.Throws<DomainException>(() => ShowProfileRules.Validate([Profile("lupl"), Profile("LUPL")]));
        Assert.Contains("LUPL", ex.Message);
    }

    [Fact]
    public void Empty_list_passes() => ShowProfileRules.Validate([]);
}
