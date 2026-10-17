namespace RedAnts.Show.Features.Remote;

public interface IDJRooms
{
    IReadOnlyList<DJRoom> All();
    Task RecordAsync(string room);
    Task DeleteAsync(string room);
}
