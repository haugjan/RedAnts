using RedAnts.DJ.Features.Admin;

namespace RedAnts.Show.Features.Music.Infrastructure;

public sealed class DJSoundSearch(GetSpotifyHits.Handler hits) : ISoundSearch
{
    public async Task<IReadOnlyList<SoundHit>> FindAsync(string term, string? reference, int limit)
    {
        var found = await hits.HandleAsync(new GetSpotifyHits.Query(term, reference, limit));
        return found.Select(h => new SoundHit(h.Uri, h.Name, h.Artist, h.Kind)).ToList();
    }
}
