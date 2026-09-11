namespace RedAnts.Show.Features.Remote;

public sealed record ShowRoom(string Name, DateTimeOffset? LastSeen);

public static class GetShowRooms
{
    public sealed record Query;

    public sealed class Handler(IShowRooms rooms)
    {
        public Task<IReadOnlyList<ShowRoom>> HandleAsync(Query query) => Task.FromResult(rooms.All());
    }
}
