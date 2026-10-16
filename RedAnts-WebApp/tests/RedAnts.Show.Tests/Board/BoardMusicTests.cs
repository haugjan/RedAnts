using RedAnts.Show.Features.Board;
using Xunit;

namespace RedAnts.Show.Tests.Board;

public class BoardMusicTests
{
    const string Hall = "win4";

    [Fact]
    public void Nothing_plays_before_a_board_asks_for_it()
    {
        var music = new ShowBoardMusic();

        var state = music.Of(Hall);

        Assert.Equal("", state.MusicId);
        Assert.True(state.PlayedBy("irgendein-bord"));
    }

    [Fact]
    public void The_board_that_starts_a_piece_owns_it()
    {
        var music = new ShowBoardMusic();

        music.Play(Hall, "bord-a", "m5");
        var state = music.Of(Hall);

        Assert.Equal("m5", state.MusicId);
        Assert.True(state.PlayedBy("bord-a"));
        Assert.False(state.PlayedBy("bord-b"));
    }

    [Fact]
    public void A_second_board_taking_over_moves_the_order_so_the_first_one_stops()
    {
        var music = new ShowBoardMusic();
        music.Play(Hall, "bord-a", "m5");
        var before = music.Of(Hall);

        music.Play(Hall, "bord-b", "m6");
        var after = music.Of(Hall);

        Assert.True(after.MusicOrder > before.MusicOrder);
        Assert.False(after.PlayedBy("bord-a"));
    }

    [Fact]
    public void Stop_from_any_board_clears_the_hall()
    {
        var music = new ShowBoardMusic();
        music.Play(Hall, "bord-a", "m5");

        music.Command(Hall, "bord-b", "stop");
        var state = music.Of(Hall);

        Assert.Equal("", state.MusicId);
        Assert.Equal("stop", state.Control);
        Assert.Equal(1, state.ControlOrder);
    }

    [Fact]
    public void A_fade_leaves_the_order_alone_so_the_piece_can_fade_out()
    {
        var music = new ShowBoardMusic();
        music.Play(Hall, "bord-a", "m5");
        var playing = music.Of(Hall);

        music.Command(Hall, "bord-a", "fade");
        var state = music.Of(Hall);

        Assert.Equal(playing.MusicOrder, state.MusicOrder);
        Assert.Equal("fade", state.Control);
        Assert.Equal("", state.MusicId);
    }

    [Fact]
    public void Every_other_control_keeps_the_running_piece()
    {
        var music = new ShowBoardMusic();
        music.Play(Hall, "bord-a", "m5");

        music.Command(Hall, "bord-a", "next");
        var state = music.Of(Hall);

        Assert.Equal("m5", state.MusicId);
        Assert.Equal("next", state.Control);
    }

    [Fact]
    public void Claiming_does_not_restart_or_stop_the_music()
    {
        var music = new ShowBoardMusic();
        music.Play(Hall, "bord-a", "m5");
        var playing = music.Of(Hall);

        music.Claim(Hall, "bord-b");
        var state = music.Of(Hall);

        Assert.Equal(playing.MusicOrder, state.MusicOrder);
        Assert.Equal("m5", state.MusicId);
        Assert.False(state.PlayedBy("bord-a"));
    }

    [Fact]
    public void Halls_keep_their_own_music()
    {
        var music = new ShowBoardMusic();

        music.Play(Hall, "bord-a", "m5");
        music.Play("oberseen", "bord-b", "m6");

        Assert.Equal("m5", music.Of(Hall).MusicId);
        Assert.Equal("m6", music.Of("oberseen").MusicId);
    }

    [Fact]
    public void Each_change_tells_the_open_boards()
    {
        var music = new ShowBoardMusic();
        var seen = new List<string>();
        music.Changed += hall => seen.Add(hall);

        music.Play(Hall, "bord-a", "m5");
        music.Command(Hall, "bord-a", "stop");

        Assert.Equal([Hall, Hall], seen);
    }
}
