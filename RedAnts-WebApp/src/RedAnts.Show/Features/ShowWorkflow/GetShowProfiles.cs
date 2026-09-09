using RedAnts.Show.Domain;
using RedAnts.Show.Features.Ports;

namespace RedAnts.Show.Features.ShowWorkflow;

public static class GetShowProfiles
{
    public sealed record Query;

    public sealed class Handler(IShowProfiles profiles)
    {
        public Task<IReadOnlyList<ShowProfile>> HandleAsync(Query query) => profiles.GetAllAsync();
    }
}
