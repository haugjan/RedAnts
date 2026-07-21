using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public interface IHelpers
{
    Task<IReadOnlyList<Helper>> GetBySeasonAsync(int seasonId);
    Task<Helper?> FindByIdAsync(int id);
    Task<Helper?> FindByPasswordAsync(string code);
    Task<Helper> AddAsync(int seasonId, string firstName, string lastName, string email);
    Task SetActiveAsync(int id, bool active);
    Task SetAssignmentAsync(int id, bool allEvents, IReadOnlyList<int> eventIds);
    Task DeleteAsync(int id);
}
