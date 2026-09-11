namespace RedAnts.Show.Features.Remote;

public static class RecordShowRoom
{
    public sealed record Command(string? Room);

    public sealed class Handler(IShowRooms rooms)
    {
        public Task HandleAsync(Command command) =>
            string.IsNullOrWhiteSpace(command.Room) ? Task.CompletedTask : rooms.RecordAsync(command.Room);
    }
}
