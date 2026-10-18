using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Setup;

public interface IShowSetup
{
    Task<ShowSetup> GetAsync();

    Task SaveAsync(ShowSetup setup);
}
