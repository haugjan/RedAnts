using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Halls;

public static class GetHall
{
    public sealed record Query(string Id);

    public sealed class Handler(IHallRepository halls)
    {
        public Task<Hall?> HandleAsync(Query query) => halls.GetByIdAsync(query.Id);
    }
}
