using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;

namespace RedAnts.Show.Features.TabBoards;

public static class ChooseBoardSession
{
    public sealed record Command(string HallId, BoardSession Session);

    public sealed class Handler(IBoardSessionRepository sessions, ITabBoardRepository boards, AgentSync sync)
    {
        public async Task<BoardSession> HandleAsync(Command command)
        {
            var board = await boards.GetAsync();
            var session = command.Session;
            if (!BoardTabs.Known(session.Tab))
                throw new ValidationException(nameof(BoardSession.Tab), $"Den Tab „{session.Tab}“ gibt es nicht.");
            if (board.Leagues.All(l => l.Id != session.League))
                throw new ValidationException(nameof(BoardSession.League), "Bitte eine Liga wählen.");
            if (session.Venue.Length > 0 && board.Venues.All(v => v.Id != session.Venue))
                throw new ValidationException(nameof(BoardSession.Venue), "Bitte eine Halle wählen.");
            await sessions.SaveAsync(command.HallId, session);
            await sync.PushAsync(command.HallId);
            return session;
        }
    }
}
