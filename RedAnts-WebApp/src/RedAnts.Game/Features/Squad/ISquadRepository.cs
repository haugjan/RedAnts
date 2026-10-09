using RedAnts.Game.Domain;

namespace RedAnts.Game.Features.Squad;

public interface ISquadRepository
{
    Task<Domain.Squad?> GetByManagerTokenAsync(string managerToken);

    Task SaveAsync(Domain.Squad squad);
}
