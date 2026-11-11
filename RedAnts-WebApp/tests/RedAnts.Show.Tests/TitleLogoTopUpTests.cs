using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure.Content;
using Xunit;

namespace RedAnts.Show.Tests;

public class TitleLogoTopUpTests
{
    private static ShowSetup With(params OverlayTitle[] titles) => new(titles, []);

    private static OverlayTitle Stored(string kind, params string[] fields) =>
        new(kind, "LUPL-SU_board.gtzip", 2, fields.ToDictionary(name => name, _ => "x", StringComparer.OrdinalIgnoreCase));

    [Fact]
    public void The_board_titles_learn_their_two_team_logos()
    {
        var filled = TitleLogoTopUp.Fill(With(Stored("result", "TxtMain.Text")));

        var title = Assert.Single(filled!.Titles);
        Assert.Equal("{home.logofile}", title.Fields[Hall.HomeImage]);
        Assert.Equal("{away.logofile}", title.Fields[Hall.AwayImage]);
        Assert.Equal("x", title.Fields["TxtMain.Text"]);
    }

    [Fact]
    public void A_one_team_title_learns_the_team_logo()
    {
        var filled = TitleLogoTopUp.Fill(With(Stored("bestplayer", "TxtName.Text")));

        var title = Assert.Single(filled!.Titles);
        Assert.Equal("{team.logofile}", title.Fields[Hall.TeamImage]);
        Assert.False(title.Fields.ContainsKey(Hall.HomeImage));
    }

    [Fact]
    public void A_field_that_is_already_there_is_left_alone()
    {
        var filled = TitleLogoTopUp.Fill(With(Stored("result", Hall.HomeImage)));

        Assert.NotNull(filled);
        Assert.Equal("x", filled.Titles[0].Fields[Hall.HomeImage]);
        Assert.Equal("{away.logofile}", filled.Titles[0].Fields[Hall.AwayImage]);
    }

    [Fact]
    public void Nothing_to_add_means_nothing_is_saved() =>
        Assert.Null(TitleLogoTopUp.Fill(With(Stored("result", Hall.HomeImage, Hall.AwayImage, "TxtMain.Text"),
            Stored("coach", Hall.TeamImage))));

    [Fact]
    public void A_title_nobody_ships_is_left_alone() =>
        Assert.Null(TitleLogoTopUp.Fill(With(Stored("eigener-titel", "TxtMain.Text"))));

    [Fact]
    public void A_text_field_the_hall_deleted_stays_deleted()
    {
        var filled = TitleLogoTopUp.Fill(With(Stored("result", "TxtMain.Text")));

        var names = Assert.Single(filled!.Titles).Fields.Keys.Order(StringComparer.Ordinal).ToList();
        Assert.Equal([Hall.AwayImage, Hall.HomeImage, "TxtMain.Text"], names);
    }
}
