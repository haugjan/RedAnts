using RedAnts.DJ.Domain;
using RedAnts.DJ.Features.Board;
using Xunit;

namespace RedAnts.DJ.Tests;

public class GetDJTilesTests
{
    private sealed class Profiles(IReadOnlyList<DJProfile> all) : IDJProfiles
    {
        public Task<IReadOnlyList<DJProfile>> GetAllAsync() => Task.FromResult(all);
        public Task SaveAllAsync(IReadOnlyList<DJProfile> profiles) => Task.CompletedTask;
    }

    [Fact]
    public async Task Flattens_playable_tiles_across_folders_and_profiles()
    {
        var jingle = new DJButton("t1", "Tor-Jingle", Color: "#FF7C80", Sound: new DJSound(SoundKind.Local, "tor.mp3"));
        var playlist = new DJButton("t2", "Warmup", Songs: [new DJSound(SoundKind.Spotify, "spotify:track:a"), new DJSound(SoundKind.Spotify, "spotify:track:b")], SongsRandom: true);
        var folder = new DJButton("f1", "Pause", Children: [playlist, new DJButton("empty", "Leer")]);
        var control = DJProfileLayout.ControlTile("root-fade", DJControl.Fade);
        var profiles = new Profiles([
            new DJProfile("heim", "Heimspiel", null, [jingle, folder, control]),
            new DJProfile("training", "Training", null, [new DJButton("t3", "Loop", Pool: [new DJSound(SoundKind.Local, "loop.mp3")])]),
        ]);

        var tiles = await new GetDJTiles.Handler(profiles).HandleAsync(new GetDJTiles.Query());

        Assert.Equal(["t1", "t2", "t3"], tiles.Select(t => t.Id));
        Assert.Equal("", tiles[0].Path);
        Assert.Equal("Pause", tiles[1].Path);
        Assert.Equal(("heim", "Heimspiel"), (tiles[1].ProfileId, tiles[1].ProfileName));
        Assert.Equal("#FF7C80", tiles[0].Color);
    }
}
