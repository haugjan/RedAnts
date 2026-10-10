namespace RedAnts.DJ.Features.Remote;

public static class RecordDJRoom
{
    public sealed record Command(string? Room);

    public sealed class Handler(IDJRooms rooms)
    {
        public Task HandleAsync(Command command) =>
            string.IsNullOrWhiteSpace(command.Room) ? Task.CompletedTask : rooms.RecordAsync(command.Room);
    }
}
