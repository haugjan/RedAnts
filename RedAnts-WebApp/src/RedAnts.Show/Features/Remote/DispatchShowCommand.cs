using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Remote;

public static class DispatchShowCommand
{
    public sealed record Command(ShowCommand Remote);

    public sealed class Handler(IShowRemote remote)
    {
        public Task<int> HandleAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.Remote.Action)) throw new DomainException("Ein Show-Befehl braucht eine Aktion.");
            return remote.DispatchAsync(command.Remote);
        }
    }
}
