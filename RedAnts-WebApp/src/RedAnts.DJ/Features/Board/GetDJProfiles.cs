using RedAnts.DJ.Domain;

namespace RedAnts.DJ.Features.Board;

public static class GetDJProfiles
{
    public sealed record Query;

    public sealed class Handler(IDJProfiles profiles)
    {
        public Task<IReadOnlyList<DJProfile>> HandleAsync(Query query) => profiles.GetAllAsync();
    }
}
