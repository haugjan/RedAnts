using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Board;

public static class GetShowProfiles
{
    public sealed record Query;

    public sealed class Handler(IShowProfiles profiles)
    {
        public Task<IReadOnlyList<ShowProfile>> HandleAsync(Query query) => profiles.GetAllAsync();
    }
}
