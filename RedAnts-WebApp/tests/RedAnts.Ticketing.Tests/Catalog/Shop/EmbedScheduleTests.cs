using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Catalog.Shop;
using Xunit;

namespace RedAnts.Ticketing.Tests.Catalog.Shop;

public class EmbedScheduleTests
{
    private static EmbedGame Game(int day, bool isAway) =>
        new($"Spiel {day}", new DateOnly(2026, 10, day), new TimeOnly(18, 0), false,
            null, null, isAway ? "Auswärtshalle" : null, isAway ? null : "/tickets/event/x", isAway);

    private static Task<IReadOnlyList<EmbedGame>> Schedule(params EmbedGame[] upcoming) =>
        new GetEmbedSchedule.Handler(new StubEmbedSchedule(upcoming)).HandleAsync(new GetEmbedSchedule.Query());

    [Fact]
    public async Task A_home_game_coming_first_is_the_only_slide()
    {
        var slides = await Schedule(Game(3, false), Game(10, true), Game(17, false));

        Assert.Single(slides);
        Assert.False(slides[0].IsAway);
        Assert.Equal("Spiel 3", slides[0].Title);
    }

    [Fact]
    public async Task Away_games_run_up_to_and_including_the_first_home_game()
    {
        var slides = await Schedule(Game(3, true), Game(5, true), Game(10, false), Game(17, true), Game(20, false));

        Assert.Equal(3, slides.Count);
        Assert.Equal(["Spiel 3", "Spiel 5", "Spiel 10"], slides.Select(s => s.Title));
        Assert.False(slides[^1].IsAway);
    }

    [Fact]
    public async Task Without_a_home_game_only_the_away_games_are_shown()
    {
        var slides = await Schedule(Game(3, true), Game(5, true));

        Assert.Equal(2, slides.Count);
        Assert.All(slides, slide => Assert.True(slide.IsAway));
    }

    [Fact]
    public async Task Without_any_game_the_schedule_stays_empty()
    {
        Assert.Empty(await Schedule());
    }
}

internal sealed class StubEmbedSchedule(IReadOnlyList<EmbedGame> upcoming) : IEmbedScheduleReader
{
    public Task<IReadOnlyList<EmbedGame>> GetUpcomingAsync() => Task.FromResult(upcoming);
}
