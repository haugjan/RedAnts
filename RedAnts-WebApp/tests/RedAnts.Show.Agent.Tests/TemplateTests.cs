using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Keys;

namespace RedAnts.Show.Agent.Tests;

public class TemplateTests
{
    static readonly Game Game = Game.None with
    {
        Title = "Red Ants - Kloten",
        Home = GameTeam.Empty with
        {
            LongName = "Red Ants Winterthur",
            ShortName = "RAW",
            Color = "#D02D38",
            Coach = "Trainerin",
            Players = [new Player(41, "Livia Werz", "Goalie"), new Player(7, "Nora Stark", "Stürmerin", Sponsor: "Kempa")],
            Starting6 = [41, 7],
            TopScorer = 7,
        },
        Away = GameTeam.Empty with { LongName = "Kloten-Dietlikon Jets", ShortName = "KDJ", Color = "#2A6EBB" },
        Referee1 = "M. Muster",
    };

    static LiveGame Live()
    {
        var live = new LiveGame();
        live.AdjustScore("home", 3);
        live.AdjustScore("away", 1);
        return live;
    }

    [Fact]
    public void A_player_fills_number_name_sponsor_and_the_team()
    {
        var live = Live();
        var ctx = new KeyContext("home", Game.Home.Find(7), Event: "goal");

        var text = Templates.Expand("{player.number} {player.name} ({player.sponsor}) {team.short} {score} {event} {referee1}", ctx, Game, live);

        Assert.Equal("7 Nora Stark (Kempa) RAW 3:1 goal M. Muster", text);
    }

    [Fact]
    public void The_led_playlist_takes_the_number_with_two_digits()
    {
        var ctx = new KeyContext("home", Game.Home.Find(7));

        Assert.Equal("STR_RA_EIN_07", Templates.Expand("STR_RA_EIN_{player.number00}", ctx, Game, Live()));
        Assert.Equal("STR_RA_07", Templates.Expand("STR_RA_{player.number00}", ctx, Game, Live()));
        Assert.Equal("STR_RA_", Templates.Expand("STR_RA_{player.number00}", KeyContext.None, Game, Live()));
    }

    static readonly PersonalSponsors Sponsors = new(
        @"C:\Livestream\PersonalSponsors", @"C:\Livestream\PersonalSponsorsAway",
        new Dictionary<string, int> { ["41"] = 2, ["00"] = 1, ["coach"] = 1 },
        new Dictionary<string, int> { ["9"] = 1 },
        OnPenalty: false);

    [Fact]
    public void A_goal_takes_the_first_sponsor_logo_of_the_number_from_the_home_folder()
    {
        var ctx = new KeyContext("home", Game.Home.Find(41));

        Assert.Equal(@"C:\Livestream\PersonalSponsors\41_1.png", Templates.Expand("{player.sponsor}", ctx, Game, Live(), "", Sponsors, "player_event"));
        Assert.Equal("#FFFFFFFF", Templates.Expand("{sponsor.background}", ctx, Game, Live(), "", Sponsors, "player_event"));
        Assert.Equal("#FFF000FF", Templates.Expand("{sponsor.border}", ctx, Game, Live(), "", Sponsors, "player_event"));
    }

    [Fact]
    public void The_topscorer_is_looked_up_as_00_like_tcunihockey_does()
    {
        var ctx = new KeyContext("home", Game.Home.Find(7)! with { Sponsor = null });

        Assert.Equal(@"C:\Livestream\PersonalSponsors\00_1.png", Templates.Expand("{player.sponsor}", ctx, Game, Live(), "", Sponsors, "name"));
    }

    [Fact]
    public void Without_an_entry_the_sponsor_box_stays_empty_and_transparent()
    {
        var game = Game with { Home = Game.Home with { Players = [new Player(14, "Ohne Sponsor")] } };
        var ctx = new KeyContext("home", game.Home.Find(14));

        Assert.Equal("", Templates.Expand("{player.sponsor}", ctx, game, Live(), "", Sponsors, "player_event"));
        Assert.Equal("#FFFFFF00", Templates.Expand("{sponsor.background}", ctx, game, Live(), "", Sponsors, "player_event"));
        Assert.Equal("#FFF00000", Templates.Expand("{sponsor.border}", ctx, game, Live(), "", Sponsors, "player_event"));
    }

    [Fact]
    public void A_title_the_hall_switched_off_shows_no_sponsor()
    {
        var ctx = new KeyContext("home", Game.Home.Find(41));

        Assert.Equal("", Templates.Expand("{player.sponsor}", ctx, Game, Live(), "", Sponsors, "penalty"));
        Assert.Equal(@"C:\Livestream\PersonalSponsors\41_1.png", Templates.Expand("{player.sponsor}", ctx, Game, Live(), "", Sponsors, "bestplayer"));
    }

    [Fact]
    public void The_coach_title_and_the_away_side_use_their_own_key_and_folder()
    {
        Assert.Equal(@"C:\Livestream\PersonalSponsors\coach_1.png", Templates.Expand("{player.sponsor}", new KeyContext("home"), Game, Live(), "", Sponsors, "coach"));
        var away = Game with { Away = Game.Away with { Players = [new Player(9, "Gast Neun")] } };
        Assert.Equal(@"C:\Livestream\PersonalSponsorsAway\9_1.png", Templates.Expand("{player.sponsor}", new KeyContext("away", away.Away.Find(9)), away, Live(), "", Sponsors, "player_event"));
        Assert.Equal("", Templates.Expand("{player.sponsor}", new KeyContext("home", Game.Home.Find(41)), Game, Live(), "", null, "player_event"));
    }

    [Fact]
    public void The_team_logos_become_file_paths_from_the_image_folder()
    {
        var ctx = new KeyContext("away");
        const string folder = @"C:\Livestream\Einblender\TeamLogos";

        Assert.Equal(@"C:\Livestream\Einblender\TeamLogos\KDJ.png", Templates.Expand("{team.logofile}", ctx, Game, Live(), folder));
        Assert.Equal(@"C:\Livestream\Einblender\TeamLogos\RAW.png", Templates.Expand("{home.logofile}", ctx, Game, Live(), folder));
        Assert.Equal(@"C:\Livestream\Einblender\TeamLogos\KDJ.png", Templates.Expand("{away.logofile}", ctx, Game, Live(), folder));
    }

    [Fact]
    public void Without_an_image_folder_no_logo_path_is_invented() =>
        Assert.Equal("", Templates.Expand("{home.logofile}", new KeyContext("home"), Game, Live()));

    [Fact]
    public void The_side_decides_which_team_the_short_name_comes_from()
    {
        var live = Live();
        var ctx = new KeyContext("away");

        Assert.Equal("KDJ", Templates.Expand("{team.short}", ctx, Game, live));
        Assert.Equal("Red Ants Winterthur", Templates.Expand("{home.long}", ctx, Game, live));
    }

    [Fact]
    public void The_team_colour_goes_out_as_well()
    {
        Assert.Equal("#D02D38", Templates.Expand("{home.color}", new KeyContext("home"), Game, Live()));
        Assert.Equal("#2A6EBB", Templates.Expand("{away.color}", new KeyContext("home"), Game, Live()));
    }

    [Fact]
    public void The_topscorer_is_the_player_behind_the_entry()
    {
        var text = Templates.Expand("{topscorer.number} {topscorer.name}", new KeyContext("home"), Game, Live());

        Assert.Equal("7 Nora Stark", text);
    }

    [Fact]
    public void Penalty_and_free_text_come_from_the_key()
    {
        var ctx = new KeyContext("home", Penalty: "2+2", Text: "Hallo");

        Assert.Equal("2+2 Hallo", Templates.Expand("{penalty} {text}", ctx, Game, Live()));
    }

    [Fact]
    public void The_starting_six_can_be_read_position_by_position()
    {
        var text = Templates.Expand("{starting6.1.number} {starting6.1.name} / {starting6.2.number}", new KeyContext("home"), Game, Live());

        Assert.Equal("41 Livia Werz / 7", text);
    }

    [Fact]
    public void A_text_without_braces_stays_as_it_is()
    {
        Assert.Equal("Guten Abend", Templates.Expand("Guten Abend", KeyContext.None, Game, Live()));
    }
}
