using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Remote;

public interface IShowRemote
{
    IDisposable Register(string? room, Func<ShowCommand, Task> handler);
    Task<int> DispatchAsync(ShowCommand command);
}
