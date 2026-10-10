using RedAnts.DJ.Domain;

namespace RedAnts.DJ.Features.Remote;

public interface IDJRemote
{
    IDisposable Register(string? room, Func<DJCommand, Task> handler);
    Task<int> DispatchAsync(DJCommand command);
}
