namespace RedAnts.DJ.Features.Admin;

public sealed record SpotifyHit(string Uri, string Name, string Artist, string Kind = "track");

public static class GetSpotifyHits
{
    public sealed record Query(string Term, string? Reference = null, int Limit = 10);

    public sealed class Handler(IDJSpotifySearch spotify)
    {
        public async Task<IReadOnlyList<SpotifyHit>> HandleAsync(Query query)
        {
            if (!spotify.Configured) return [];
            if (query.Reference is { Length: > 0 } reference)
            {
                var parsed = Domain.DJSpotifyLink.Parse(reference);
                if (parsed is null) return [];
                if (parsed.Value.Kind == "track")
                    return await spotify.GetTrackAsync(reference) is { } track
                        ? [new SpotifyHit(track.Uri, track.Name, track.Artist)]
                        : [];
                return await spotify.GetContextAsync(reference) is { } context
                    ? [new SpotifyHit(context.Uri, context.Name, context.Owner, context.Kind)]
                    : [];
            }
            if (string.IsNullOrWhiteSpace(query.Term)) return [];
            var hits = await spotify.SearchAsync(query.Term.Trim(), query.Limit);
            return hits.Select(h => new SpotifyHit(h.Uri, h.Name, h.Artist)).ToList();
        }
    }
}
