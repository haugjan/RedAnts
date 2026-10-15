namespace RedAnts.Show.Features.Games;

public interface IGameReader
{
    Task<IReadOnlyList<GameRow>> ListAsync();
}
