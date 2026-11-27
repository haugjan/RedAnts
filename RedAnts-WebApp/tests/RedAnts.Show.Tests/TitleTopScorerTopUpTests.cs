using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure.Content;
using Xunit;

namespace RedAnts.Show.Tests;

public class TitleTopScorerTopUpTests
{
    private static ShowSetup With(params OverlayTitle[] titles) => new(titles, []);

    private static OverlayTitle Stored(string kind, params string[] fields) =>
        new(kind, "LUPL-SU_team_player_event.gtzip", 1, fields.ToDictionary(name => name, _ => "x", StringComparer.OrdinalIgnoreCase));

    [Fact]
    public void The_four_player_titles_learn_the_topscorer_icon_switch()
    {
        var filled = TitleTopScorerTopUp.Fill(With(Stored("player_event", "TxtNumber.Text"), Stored("penalty"), Stored("name"), Stored("bestplayer")));

        Assert.All(filled!.Titles, title => Assert.Equal("{player.topscorer}", title.Fields[Hall.TopScorerVisible]));
        Assert.Equal("x", filled.Titles[0].Fields["TxtNumber.Text"]);
    }

    [Fact]
    public void The_coach_and_the_board_titles_stay_as_they_are() =>
        Assert.Null(TitleTopScorerTopUp.Fill(With(Stored("coach", Hall.TeamImage), Stored("result", "TxtMain.Text"))));

    [Fact]
    public void A_title_that_already_switches_the_icon_is_not_saved_again() =>
        Assert.Null(TitleTopScorerTopUp.Fill(With(Stored("name", Hall.TopScorerVisible))));

    [Fact]
    public void The_shipped_titles_switch_the_icon_only_where_a_player_is_shown()
    {
        foreach (var kind in Hall.PlayerTitles)
            Assert.Equal("{player.topscorer}", Hall.DefaultOverlayTitles.Single(t => t.Kind == kind).Fields[Hall.TopScorerVisible]);
        Assert.False(Hall.DefaultOverlayTitles.Single(t => t.Kind == "coach").Fields.ContainsKey(Hall.TopScorerVisible));
        Assert.Contains(SpecialKey.All, key => key.Id == SpecialKey.SponsorNext && SpecialKey.NeedsVmix(key.Id));
    }
}
