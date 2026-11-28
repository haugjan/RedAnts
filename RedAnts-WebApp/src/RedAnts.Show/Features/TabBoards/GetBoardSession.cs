using RedAnts.Show.Domain;
using RedAnts.Show.Features.Games;
using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.TabBoards;

public static class GetBoardSession
{
    public sealed record Query(string HallId);

    public sealed class Handler(IBoardSessionRepository sessions, ITabBoardRepository boards, IHallRepository halls, IGameRepository games)
    {
        public async Task<BoardSession> HandleAsync(Query query)
        {
            if (await sessions.GetAsync(query.HallId) is { } session) return session;
            var hall = await halls.GetByIdAsync(query.HallId);
            var game = hall?.ActiveGameId is { } id ? await games.GetByIdAsync(id) : null;
            return BoardSession.Off((await boards.GetAsync()).LeagueOf(game?.League));
        }
    }
}
