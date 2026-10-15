using RedAnts.Show.Features.Games;
using Xunit;

namespace RedAnts.Show.Tests;

public class GameScheduleTests
{
    private static readonly DateTime Today = new(2026, 10, 14, 0, 0, 0, DateTimeKind.Unspecified);

    private static UpcomingGame Game(string id, DateTime? date, string result = "") =>
        new(id, date, "Red Ants", "Gegner", "Win4", result);

    [Fact]
    public void Games_of_the_last_six_weeks_come_back_newest_first()
    {
        var games = new[]
        {
            Game("a", Today.AddDays(-2), "5:3"),
            Game("b", Today.AddDays(-20), "2:4"),
            Game("c", Today.AddDays(-50)),
        };

        var schedule = GetGameSchedule.Handler.Split(games, Today, GetGameSchedule.DefaultDaysBack);

        Assert.Equal(["a", "b"], schedule.Played.Select(g => g.Id));
        Assert.Equal("5:3", schedule.Played[0].Result);
        Assert.Empty(schedule.Next);
    }

    [Fact]
    public void Coming_games_stay_in_order_and_a_game_without_a_date_counts_as_coming()
    {
        var games = new[]
        {
            Game("spaet", Today.AddDays(9)),
            Game("heute", Today),
            Game("ohne", null),
            Game("bald", Today.AddDays(3)),
        };

        var schedule = GetGameSchedule.Handler.Split(games, Today, GetGameSchedule.DefaultDaysBack);

        Assert.Equal(["ohne", "heute", "bald", "spaet"], schedule.Next.Select(g => g.Id));
        Assert.Empty(schedule.Played);
    }

    [Fact]
    public void The_window_can_be_widened()
    {
        var games = new[] { Game("alt", Today.AddDays(-80)) };

        Assert.Empty(GetGameSchedule.Handler.Split(games, Today, 42).Played);
        Assert.Single(GetGameSchedule.Handler.Split(games, Today, 120).Played);
    }
}
