using RedAnts.Domain;
using RedAnts.Show.Domain;
using Xunit;

namespace RedAnts.Show.Tests;

public class PersonalSponsorsTests
{
    private const string Ini = "[Home]\n7=2\n00=1\ncoach=1\n\n[Away]\n9=1\n";

    [Fact]
    public void The_tcunihockey_file_is_read_section_by_section()
    {
        var sponsors = PersonalSponsors.Parse(Ini) with { HomeFolder = @"C:\Livestream\PersonalSponsors" };

        Assert.Equal(2, sponsors.Count("home", "7"));
        Assert.Equal(1, sponsors.Count("home", "00"));
        Assert.Equal(1, sponsors.Count("home", "coach"));
        Assert.Equal(1, sponsors.Count("away", "9"));
        Assert.Equal(0, sponsors.Count("home", "9"));
        Assert.Equal(@"C:\Livestream\PersonalSponsors\7_2.png", sponsors.Logo("home", "7", 2));
        Assert.Null(sponsors.Logo("home", "7", 3));
        Assert.Null(sponsors.Logo("away", "9"));
    }

    [Fact]
    public void Leading_zeros_fall_away_except_for_the_topscorer()
    {
        var sponsors = PersonalSponsors.Parse("07=1\n00=2\nCoach=1");

        Assert.Equal(1, sponsors.Count("home", "7"));
        Assert.Equal(2, sponsors.Count("home", "00"));
        Assert.Equal(1, sponsors.Count("home", "coach"));
    }

    [Fact]
    public void The_file_comes_back_in_the_same_shape()
    {
        var sponsors = PersonalSponsors.Parse(Ini);

        Assert.Equal("[Home]\n00=1\n7=2\ncoach=1\n[Away]\n9=1", sponsors.Ini);
        Assert.Equal(sponsors.Ini, PersonalSponsors.Parse(sponsors.Ini).Ini);
        Assert.Equal("[Home]\n[Away]", PersonalSponsors.None.Ini);
    }

    [Theory]
    [InlineData("7")]
    [InlineData("7=viele")]
    [InlineData("[Gast]")]
    public void A_line_that_is_not_number_equals_count_is_refused(string line) =>
        Assert.Throws<ValidationException>(() => PersonalSponsors.Parse(line));

    [Fact]
    public void The_switches_decide_which_titles_show_the_sponsor()
    {
        var sponsors = new PersonalSponsors(OnPenalty: false, OnName: false);

        Assert.True(sponsors.Shows("player_event"));
        Assert.False(sponsors.Shows("penalty"));
        Assert.False(sponsors.Shows("name"));
        Assert.True(sponsors.Shows("bestplayer"));
        Assert.True(sponsors.Shows("coach"));
        Assert.False(sponsors.Shows("timeout"));
    }

    [Fact]
    public void Every_player_title_ships_with_the_sponsor_fields()
    {
        foreach (var kind in Hall.SponsoredTitles)
        {
            var title = Hall.DefaultOverlayTitles.Single(t => t.Kind == kind);
            Assert.Equal("{player.sponsor}", title.Fields[Hall.SponsorImage]);
            Assert.Equal("{sponsor.background}", title.Fields[Hall.SponsorBackground]);
            Assert.Equal("{sponsor.border}", title.Fields[Hall.SponsorBorder]);
        }
        Assert.False(Hall.DefaultOverlayTitles.Single(t => t.Kind == "timeout").Fields.ContainsKey(Hall.SponsorImage));
    }

    [Fact]
    public void A_hall_without_sponsors_answers_with_empty_values()
    {
        var hall = Hall.Create("win4", "WIN 4");

        Assert.Same(PersonalSponsors.None, hall.SponsorLogos);
        Assert.Null(hall.SponsorLogos.Logo("home", "7"));
        Assert.False(hall.SponsorLogos.Any);
    }
}
