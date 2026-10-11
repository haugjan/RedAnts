namespace RedAnts.Show.Features.Music;

public sealed record SoundHit(string Uri, string Name, string Artist, string Kind);

public interface ISoundSearch
{
    Task<IReadOnlyList<SoundHit>> FindAsync(string term, string? reference, int limit);
}
