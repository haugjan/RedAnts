using RedAnts.DJ.Domain;

namespace RedAnts.DJ.Features.Remote;

public static class DispatchDJCommand
{
    public sealed record Command(DJCommand Remote);

    public sealed class Handler(IDJRemote remote)
    {
        public Task<int> HandleAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.Remote.Action)) throw new DomainException("Ein DJ-Befehl braucht eine Aktion.");
            return remote.DispatchAsync(command.Remote);
        }
    }
}
