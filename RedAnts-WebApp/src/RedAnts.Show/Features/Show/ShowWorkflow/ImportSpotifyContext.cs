using RedAnts.Domain.Show;
using RedAnts.Features.Show.Ports;

namespace RedAnts.Features.Show.ShowWorkflow;

public static class ImportSpotifyContext
{
    public sealed record Query(string Ref, int Max = 200);

    public sealed class Handler(IShowSpotifySearch spotify)
    {
        public Task<IReadOnlyList<SpotifyTrack>> HandleAsync(Query query) => spotify.GetContextTracksAsync(query.Ref, query.Max);
    }
}
