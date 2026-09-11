namespace RedAnts.Show.Features.Remote;

public static class DeleteShowRoom
{
    public sealed record Command(string Room);

    public sealed class Handler(IShowRooms rooms)
    {
        public Task HandleAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.Room)) throw new DomainException("Ein Raum braucht einen Namen.");
            return rooms.DeleteAsync(command.Room);
        }
    }
}
