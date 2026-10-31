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
        Assert.Contains(hall.LedTemplates, t => t.Event == "stopp");
        Assert.True(hall.AcceptsKey(hall.AgentKey));
        Assert.False(hall.AcceptsKey("nope"));
        Assert.False(hall.AcceptsKey(null));
    }

    private static string CommandOf(string led) =>
        Hall.DefaultLedTemplates.Single(t => t.Event == led).Command;

    [Theory]
    [InlineData("tor")]
    [InlineData("tor-gast")]
    [InlineData("einlauf")]
    [InlineData("best-player")]
    [InlineData("starting-six")]
    [InlineData("strafe")]
    [InlineData("timeout")]
    [InlineData("staff")]
    [InlineData("gesucht")]
    [InlineData("teamliste")]
    [InlineData("live")]
    [InlineData("replay")]
    [InlineData("logo")]
    [InlineData("gast-chur")]
    [InlineData("gast-bern")]
    [InlineData("gast-wizards")]
    [InlineData("gast-zug")]
    public void An_animation_runs_as_an_overlay_so_the_clock_stays(string led) =>
        Assert.EndsWith(" -o", CommandOf(led));

    [Theory]
    [InlineData("spiel-nla")]
    [InlineData("spiel-u21")]
    [InlineData("pause")]
    [InlineData("intermission")]
    [InlineData("pausenspiel")]
    [InlineData("intro")]
    [InlineData("sponsoren")]
    [InlineData("lidl-welcome")]
    [InlineData("mobiliar")]
    public void A_full_picture_stays_the_main_playlist(string led) =>
        Assert.DoesNotContain(" -o", CommandOf(led));

    [Fact]
    public void Every_guest_logo_overlays_the_clock()
    {
        var guests = Hall.DefaultLedTemplates.Where(t => t.Event.StartsWith("gast-")).ToList();

        Assert.Equal(9, guests.Count);
        Assert.All(guests, t => Assert.EndsWith(" -o", t.Command));
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
