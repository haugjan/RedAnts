namespace RedAnts.Show.Features.Music;

public interface IMusicLibrary
{
    Task<IReadOnlyList<MusicItem>> AllAsync();

    Task<IReadOnlyList<MusicNode>> TreeAsync();
}
