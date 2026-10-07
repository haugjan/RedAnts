namespace RedAnts.Ticketing.Features.Catalog.Shop;

public sealed record EmbedGame(
    string Title,
    DateOnly Date,
    TimeOnly StartTime,
    bool TimeUnknown,
    string? LeftLogoUrl,
    string? RightLogoUrl,
    string? Place,
    string? TicketsUrl,
    bool IsAway);

public static class GetEmbedSchedule
{
    public sealed record Query;

    public sealed class Handler(IEmbedScheduleReader reader)
    {
        public async Task<IReadOnlyList<EmbedGame>> HandleAsync(Query query)
        {
            var upcoming = await reader.GetUpcomingAsync();
            var slides = upcoming.TakeWhile(game => game.IsAway).ToList();

            var firstHome = upcoming.Skip(slides.Count).FirstOrDefault();
            if (firstHome is not null) slides.Add(firstHome);

            return slides;
        }
    }
}
