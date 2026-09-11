namespace RedAnts.Show.Features.Remote;

public interface IShowRooms
{
    IReadOnlyList<ShowRoom> All();
    Task RecordAsync(string room);
    Task DeleteAsync(string room);
}
