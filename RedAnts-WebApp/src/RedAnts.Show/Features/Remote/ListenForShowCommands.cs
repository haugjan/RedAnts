using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Remote;

public static class ListenForShowCommands
{
    public sealed record Command(string? Room, Func<ShowCommand, Task> OnCommand);

    public sealed class Handler(IShowRemote remote)
    {
        public Task<IDisposable> HandleAsync(Command command) =>
            Task.FromResult(remote.Register(command.Room, command.OnCommand));
    }
}
