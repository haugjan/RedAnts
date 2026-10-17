using RedAnts.Show.Agent.Clock;

namespace RedAnts.Show.Agent.Tests;

public class GoalScoreTests
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(25);

    private static ClockState Clock(string home, string away) =>
        ICastProtocol.Parse($"12:34;{home};{away};2;;;;;;;RA;GEG;GAME TIME")!;

    [Fact]
    public void A_goal_the_timekeeper_has_not_counted_yet_is_counted_in_the_insert()
    {
        var live = new LiveGame();
        live.Update(Clock("3", "1"));

        Assert.Equal("4:1", live.GoalScore("home", Window));
        Assert.Equal("3:2", live.GoalScore("away", Window));
    }

    [Fact]
    public void A_goal_the_timekeeper_was_faster_with_is_shown_as_it_stands()
    {
        var live = new LiveGame();
        live.Update(Clock("3", "1"));
        live.Update(Clock("4", "1"));

        Assert.Equal("4:1", live.GoalScore("home", Window));
    }

    [Fact]
    public void An_older_goal_of_the_other_side_does_not_count_as_this_one()
    {
        var live = new LiveGame();
        live.Update(Clock("3", "1"));
        live.Update(Clock("3", "2"));

        Assert.Equal("4:2", live.GoalScore("home", Window));
        Assert.Equal("3:2", live.GoalScore("away", Window));
    }

    [Fact]
    public void A_goal_from_an_earlier_period_of_play_is_counted_again()
    {
        var live = new LiveGame();
        live.Update(Clock("3", "1"));
        live.Update(Clock("4", "1"));

        Assert.Equal("5:1", live.GoalScore("home", TimeSpan.Zero));
    }

    [Fact]
    public void Without_a_clock_the_hand_kept_score_counts_the_goal()
    {
        var live = new LiveGame();

        Assert.Equal("1:0", live.GoalScore("home", Window));

        live.AdjustScore("home", 1);

        Assert.Equal("1:0", live.GoalScore("home", Window));
    }
}
