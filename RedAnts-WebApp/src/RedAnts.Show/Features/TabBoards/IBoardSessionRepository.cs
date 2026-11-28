using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.TabBoards;

public interface IBoardSessionRepository
{
    Task<BoardSession?> GetAsync(string hallId);

    Task SaveAsync(string hallId, BoardSession session);
}
