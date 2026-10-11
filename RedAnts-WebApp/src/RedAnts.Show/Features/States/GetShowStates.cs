using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.States;

public static class GetShowStates
{
    public sealed record Query;

    public sealed class Handler(IShowStates states)
    {
        public async Task<IReadOnlyList<ShowState>> HandleAsync(Query query)
        {
            var stored = await states.GetAllAsync();
            return stored.Count > 0 ? stored : DefaultStates.All;
        }
    }
}
