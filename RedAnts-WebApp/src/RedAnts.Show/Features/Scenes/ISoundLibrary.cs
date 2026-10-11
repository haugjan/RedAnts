namespace RedAnts.Show.Features.Scenes;

public interface ISoundLibrary
{
    Task<IReadOnlyList<SoundTile>> ListAsync();
}
