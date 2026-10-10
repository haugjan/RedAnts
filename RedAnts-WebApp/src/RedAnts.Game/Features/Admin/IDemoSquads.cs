namespace RedAnts.Game.Features.Admin;

public interface IDemoSquads
{
    Task<int> CreateAsync(int count, int budget);

    Task<int> DeleteAsync();

    Task<int> CountAsync();
}
