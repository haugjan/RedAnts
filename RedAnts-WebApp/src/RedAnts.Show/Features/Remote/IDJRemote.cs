using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Remote;

public interface IDJRemote
{
    IDisposable Register(string? room, Func<DJCommand, Task> handler);
    Task<int> DispatchAsync(DJCommand command);
}
