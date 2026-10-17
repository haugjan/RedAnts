using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Music;

public static class GetDJProfiles
{
    public sealed record Query;

    public sealed class Handler(IDJProfiles profiles)
    {
        public Task<IReadOnlyList<DJProfile>> HandleAsync(Query query) => profiles.GetAllAsync();
    }
}
