using RedAnts.DJ.Features.Board;

namespace RedAnts.Show.Features.Scenes.Infrastructure;

public sealed class DJSoundLibrary(GetDJTiles.Handler tiles) : ISoundLibrary
{
    public async Task<IReadOnlyList<SoundTile>> ListAsync() =>
        (await tiles.HandleAsync(new GetDJTiles.Query()))
            .Select(t => new SoundTile(t.Id, t.Label, t.Icon, t.Color, t.ProfileId, t.ProfileName, t.Path))
            .ToList();
}
