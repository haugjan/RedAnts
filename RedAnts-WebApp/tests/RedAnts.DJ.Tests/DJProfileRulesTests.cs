using RedAnts.Domain;
using RedAnts.DJ.Domain;
using Xunit;

namespace RedAnts.DJ.Tests;

public class DJProfileRulesTests
{
    private static DJProfile Profile(string id, string name = "Profil") => new(id, name, "#C8102E", []);

    [Fact]
    public void Distinct_profiles_pass() =>
        DJProfileRules.Validate([Profile("a"), Profile("b")]);

    [Fact]
    public void Blank_id_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => DJProfileRules.Validate([Profile(" ")]));
        Assert.Equal(DJProfileRules.BlankId, ex.Message);
    }

    [Fact]
    public void Blank_name_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => DJProfileRules.Validate([Profile("a", "")]));
        Assert.Equal(DJProfileRules.BlankName, ex.Message);
    }

    [Fact]
    public void Duplicate_ids_are_rejected_regardless_of_case()
    {
        var ex = Assert.Throws<DomainException>(() => DJProfileRules.Validate([Profile("lupl"), Profile("LUPL")]));
        Assert.Contains("LUPL", ex.Message);
    }

    [Fact]
    public void Empty_list_passes() => DJProfileRules.Validate([]);
}
