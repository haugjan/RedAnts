namespace RedAnts.Show.Features.Halls;

public interface IHallReader
{
    Task<IReadOnlyList<HallRow>> ListAsync();
}
