using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Setup;

public static class GetShowSetup
{
    public sealed record Query;

    public sealed class Handler(IShowSetup setup)
    {
        public Task<ShowSetup> HandleAsync(Query query) => setup.GetAsync();
    }
}
