using RedAnts.DJ.Domain;

namespace RedAnts.DJ.Features.Remote;

public static class ListenForDJCommands
{
    public sealed record Command(string? Room, Func<DJCommand, Task> OnCommand);

    public sealed class Handler(IDJRemote remote)
    {
        public Task<IDisposable> HandleAsync(Command command) =>
            Task.FromResult(remote.Register(command.Room, command.OnCommand));
    }
}
