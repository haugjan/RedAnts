using RedAnts.Show.Agent;
using RedAnts.Show.Agent.Clock;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Tests;

public class ClockHardeningTests
{
    const string HallTelegram = "12:34;3;1;2;7 02:00;;;;;;Red Ants;Gegner;GAME TIME;0;-6;0;-2;0;0;1";

    [Fact]
    public void Reads_the_twenty_field_telegram_of_the_hall_clock()
    {
        var state = ICastProtocol.Parse(HallTelegram);

        Assert.NotNull(state);
        Assert.Equal("12:34", state!.Time);
        Assert.Equal("GAME TIME", state.Mode);
        Assert.Equal("Gegner", state.Guest);
    }

    [Fact]
    public void Reads_tenths_in_the_last_minute()
    {
        var state = ICastProtocol.Parse("59.8;3;1;3;;;;;;;Red Ants;Gegner;GAME TIME");

        Assert.NotNull(state);
        Assert.Equal("59.8", state!.Time);
    }

    [Theory]
    [InlineData("Hallo Welt;das;ist;kein;Telegramm;;;;;;a;b;c")]
    [InlineData("12:34;drei;1;2;;;;;;;Red Ants;Gegner;GAME TIME")]
    public void Rejects_a_line_that_only_looks_like_a_telegram(string line)
    {
        Assert.Null(ICastProtocol.Parse(line));
    }

    [Fact]
    public void Rejects_a_line_with_embedded_null_characters()
    {
        Assert.Null(ICastProtocol.Parse("12:34;3;1;2;\0;;;;;;Red Ants;Gegner;GAME TIME"));
    }

    [Fact]
    public void An_empty_clock_type_still_counts_as_icast()
    {
        Assert.True(ICastProtocol.Supports(""));
        Assert.True(ICastProtocol.Supports(null));
        Assert.False(ICastProtocol.Supports("Bodet"));
    }

    [Fact]
    public void A_moving_time_marks_the_clock_as_running()
    {
        var live = new LiveGame();

        live.Update(ICastProtocol.Parse(HallTelegram)!);
        Assert.True(live.ClockRunning);

        live.Update(ICastProtocol.Parse(HallTelegram.Replace("12:34", "12:33"))!);
        Assert.True(live.ClockRunning);
        Assert.Equal("12:33", live.Time);
    }

    [Fact]
    public void The_last_minute_and_a_break_are_marked_for_the_tv_clock()
    {
        var running = ICastProtocol.Parse(HallTelegram)!;
        Assert.False(running.IsBreak);
        Assert.False(running.IsLastMinute);

        var lastMinute = ICastProtocol.Parse("59.8;3;1;3;;;;;;;RA;GEG;GAME TIME")!;
        Assert.True(lastMinute.IsLastMinute);

        var pause = ICastProtocol.Parse("02:00;3;1;2;;;;;;;RA;GEG;INTERMISSION")!;
        Assert.True(pause.IsBreak);
        Assert.Equal("PAUSE", pause.PeriodLabel);
    }

    [Fact]
    public void The_scoreboard_also_shows_a_score_kept_by_hand()
    {
        var values = VmixScoreboard.Manual("2", "1", "RA", "GEG");

        Assert.Equal("2", values["homeScore"]);
        Assert.Equal("GEG", values["awayName"]);
        Assert.False(values.ContainsKey("time"));
    }

    [Fact]
    public void The_team_logo_path_follows_the_image_folder()
    {
        var path = VmixScoreboard.LogoPath(@"C:\Livestream\Bilder", "Red Ants");

        Assert.Equal(@"C:\Livestream\Bilder\RED_ANTS.png", path);
    }
}
