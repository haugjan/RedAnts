using RedAnts.DJ.Domain;

namespace RedAnts.DJ.Features.Remote;

public static class SendDJCommand
{
    public sealed record Command(string Action, string? TileId = null, string? ProfileId = null, int? SongIndex = null, string? Room = null);

    public sealed class Handler(IDJRemote remote)
    {
        public Task<int> HandleAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.Action)) throw new DomainException("Ein DJ-Befehl braucht eine Aktion.");
            return remote.DispatchAsync(new DJCommand(command.Action.Trim().ToLowerInvariant(), command.TileId, command.ProfileId, command.SongIndex, command.Room));
        }
    }
}
