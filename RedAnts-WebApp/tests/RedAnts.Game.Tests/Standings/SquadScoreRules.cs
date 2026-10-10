using RedAnts.Game.Domain;
using Xunit;

namespace RedAnts.Game.Tests.Standings;

public class SquadScoreRules
{
    private static readonly IReadOnlyList<int>[] TwoPlayers =
    [
        new[] { 4, 7, 0, 11 },
        new[] { 1, 2, 9 },
    ];

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 9)]
    [InlineData(3, 9)]
    [InlineData(4, 11)]
    [InlineData(5, 0)]
    public void A_round_counts_only_that_game(int round, int expected) =>
        Assert.Equal(expected, SquadScore.RoundPoints(TwoPlayers, round));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 5)]
    [InlineData(2, 14)]
    [InlineData(4, 34)]
    [InlineData(9, 34)]
    public void The_total_adds_the_rounds_up_to_now(int round, int expected) =>
        Assert.Equal(expected, SquadScore.TotalPoints(TwoPlayers, round));

    [Fact]
    public void A_player_without_that_game_scores_nothing()
    {
        Assert.Equal(0, SquadScore.PointsInRound(new[] { 1, 2 }, 3));
        Assert.Equal(0, SquadScore.PointsInRound([], 1));
        Assert.Equal(0, SquadScore.PointsInRound(new[] { 1, 2 }, 0));
    }

    [Fact]
    public void Negative_points_count_against_the_squad()
    {
        IReadOnlyList<int>[] forms = [new[] { -1, 3 }, new[] { 5, -2 }];

        Assert.Equal(4, SquadScore.RoundPoints(forms, 1));
        Assert.Equal(1, SquadScore.RoundPoints(forms, 2));
        Assert.Equal(5, SquadScore.TotalPoints(forms, 2));
    }

    [Fact]
    public void The_round_strip_has_one_entry_per_round()
    {
        var strip = SquadScore.PointsByRound(TwoPlayers, 4);

        Assert.Equal(4, strip.Count);
        Assert.Equal([5, 9, 9, 11], strip);
    }

    [Fact]
    public void Rounds_played_follows_the_longest_season()
    {
        Assert.Equal(4, SquadScore.RoundsPlayed(TwoPlayers));
        Assert.Equal(0, SquadScore.RoundsPlayed([]));
    }

    [Fact]
    public void An_empty_squad_scores_nothing()
    {
        Assert.Equal(0, SquadScore.RoundPoints([], 3));
        Assert.Equal(0, SquadScore.TotalPoints([], 3));
        Assert.Empty(SquadScore.PointsByRound([], 0));
    }
}
