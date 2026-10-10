namespace RedAnts.DJ.Features.Remote;

public sealed record DJRoom(string Name, DateTimeOffset? LastSeen);

public static class GetDJRooms
{
    public sealed record Query;

    public sealed class Handler(IDJRooms rooms)
    {
        public Task<IReadOnlyList<DJRoom>> HandleAsync(Query query) => Task.FromResult(rooms.All());
    }
}
