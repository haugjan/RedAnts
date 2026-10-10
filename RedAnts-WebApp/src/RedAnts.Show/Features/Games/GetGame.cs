using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Games;

public static class GetGame
{
    public sealed record Query(string Id);

    public sealed class Handler(IGameRepository games)
    {
        public Task<Game?> HandleAsync(Query query) => games.GetByIdAsync(query.Id);
    }
}
