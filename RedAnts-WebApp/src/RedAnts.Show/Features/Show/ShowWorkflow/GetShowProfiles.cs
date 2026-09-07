using RedAnts.Domain.Show;
using RedAnts.Features.Show.Ports;

namespace RedAnts.Features.Show.ShowWorkflow;

public static class GetShowProfiles
{
    public sealed record Query;

    public sealed class Handler(IShowProfiles profiles)
    {
        public Task<IReadOnlyList<ShowProfile>> HandleAsync(Query query) => profiles.GetAllAsync();
    }
}
