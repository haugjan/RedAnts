using RedAnts.Domain;
using RedAnts.Show.Domain;
using Xunit;

namespace RedAnts.Show.Tests;

public class HallTests
{
    [Fact]
    public void Create_normalises_the_id_and_issues_a_key()
    {
        var hall = Hall.Create(" Win4 ", " Win4 Stratos ");

        Assert.Equal("win4", hall.Id);
        Assert.Equal("Win4 Stratos", hall.Name);
        Assert.Equal(32, hall.AgentKey.Length);
        Assert.Equal(Hall.RedAntsWomen, hall.TeamId);
        Assert.Equal(8099, hall.Devices.VmixPort);
        Assert.Contains(hall.LedTemplates, t => t.Event == "tor-heim");
        Assert.True(hall.AcceptsKey(hall.AgentKey));
        Assert.False(hall.AcceptsKey("nope"));
        Assert.False(hall.AcceptsKey(null));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("Halle Eins")]
    [InlineData("x_y")]
    public void Create_rejects_bad_ids(string id) =>
        Assert.Throws<ValidationException>(() => Hall.Create(id, "Name"));

    [Fact]
    public void Season_starts_in_july()
    {
        Assert.Equal(2026, Hall.SeasonOf(new DateOnly(2026, 10, 10)));
        Assert.Equal(2025, Hall.SeasonOf(new DateOnly(2026, 3, 1)));
    }

    [Fact]
    public void Renewing_the_key_changes_it()
    {
        var hall = Hall.Create("win4", "Win4");
        var renewed = hall.WithNewKey();
        Assert.NotEqual(hall.AgentKey, renewed.AgentKey);
        Assert.False(renewed.AcceptsKey(hall.AgentKey));
    }

    [Fact]
    public void Validated_rejects_an_empty_name() =>
        Assert.Throws<ValidationException>(() => (Hall.Create("win4", "Win4") with { Name = " " }).Validated());
}
