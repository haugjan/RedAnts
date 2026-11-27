using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure.Content;
using Xunit;

namespace RedAnts.Show.Tests;

public class TitleSponsorTopUpTests
{
    private static ShowSetup With(params OverlayTitle[] titles) => new(titles, []);

    private static OverlayTitle Stored(string kind, params string[] fields) =>
        new(kind, "LUPL-SU_team_player_event.gtzip", 1, fields.ToDictionary(name => name, _ => "x", StringComparer.OrdinalIgnoreCase));

    [Fact]
    public void A_player_title_learns_the_three_sponsor_fields()
    {
        var filled = TitleSponsorTopUp.Fill(With(Stored("player_event", "TxtNumber.Text", Hall.TeamImage)));

        var title = Assert.Single(filled!.Titles);
        Assert.Equal("{player.sponsor}", title.Fields[Hall.SponsorImage]);
        Assert.Equal("{sponsor.background}", title.Fields[Hall.SponsorBackground]);
        Assert.Equal("{sponsor.border}", title.Fields[Hall.SponsorBorder]);
        Assert.Equal("x", title.Fields["TxtNumber.Text"]);
    }

    [Fact]
    public void A_field_the_hall_already_set_is_left_alone()
    {
        var filled = TitleSponsorTopUp.Fill(With(Stored("coach", Hall.SponsorImage)));

        var title = Assert.Single(filled!.Titles);
        Assert.Equal("x", title.Fields[Hall.SponsorImage]);
        Assert.Equal("{sponsor.border}", title.Fields[Hall.SponsorBorder]);
    }

    [Fact]
    public void Titles_without_a_sponsor_box_and_complete_titles_are_not_saved() =>
        Assert.Null(TitleSponsorTopUp.Fill(With(
            Stored("timeout", "TxtInfo.Text"),
            Stored("result", "TxtMain.Text"),
            Stored("name", Hall.SponsorImage, Hall.SponsorBackground, Hall.SponsorBorder))));
}
