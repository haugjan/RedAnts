namespace RedAnts.DJ.Features.Remote;

public static class DeleteDJRoom
{
    public sealed record Command(string Room);

    public sealed class Handler(IDJRooms rooms)
    {
        public Task HandleAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.Room)) throw new DomainException("Ein Raum braucht einen Namen.");
            return rooms.DeleteAsync(command.Room);
        }
    }
}
