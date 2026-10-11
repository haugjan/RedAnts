using RedAnts.Domain;
using RedAnts.Show.Domain;
using Xunit;

namespace RedAnts.Show.Tests;

public class SoundCommandTests
{
    [Fact]
    public void Create_makes_a_slug_from_the_label()
    {
        var sound = SoundCommand.Create("Tor-Jingle Heim");

        Assert.Equal("tor-jingle-heim", sound.Id);
        Assert.Equal("Tor-Jingle Heim", sound.Label);
        Assert.Equal("sound:tor-jingle-heim", sound.Target);
        Assert.Empty(sound.Tracks);
    }

    [Fact]
    public void Validation_rejects_a_track_without_a_reference()
    {
        var sound = SoundCommand.Create("Pause") with { Tracks = [new SoundTrack(SoundSource.Blob, "")] };

        var error = Assert.Throws<ValidationException>(() => sound.Validated());
        Assert.Equal("Ref", error.Field);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Validation_rejects_an_impossible_volume(int volume)
    {
        var sound = SoundCommand.Create("Pause") with { Volume = volume };

        Assert.Throws<ValidationException>(() => sound.Validated());
    }

    [Fact]
    public void Validation_trims_and_keeps_the_playlist_order()
    {
        var sound = (SoundCommand.Create("Mix") with
        {
            Tracks =
            [
                new SoundTrack(SoundSource.Blob, " a.mp3 ", " Eins ", " Band ", 12),
                new SoundTrack(SoundSource.Spotify, "spotify:track:x", DurationSec: 30),
            ],
            Random = true,
        }).Validated();

        Assert.Equal(["a.mp3", "spotify:track:x"], sound.Tracks.Select(t => t.Ref));
        Assert.Equal("Eins", sound.Tracks[0].Title);
        Assert.Equal(12, sound.Tracks[0].StartSec);
        Assert.True(sound.IsPlaylist);
        Assert.True(sound.Random);
    }

    [Theory]
    [InlineData("sound:tor", "tor")]
    [InlineData("sound:", null)]
    [InlineData("tile:tor", null)]
    [InlineData("", null)]
    public void A_scene_target_names_the_sound(string target, string? expected) =>
        Assert.Equal(expected, SoundRequest.SoundOf(target));

    [Fact]
    public void A_sound_report_is_parsed_into_action_and_sound()
    {
        var request = SoundRequest.Parse("""{"action":"sound.play","target":"sound:tor","scene":"tor-heim"}""");

        Assert.NotNull(request);
        Assert.True(request.IsPlay);
        Assert.Equal("tor", request.SoundId);
        Assert.Equal("tor-heim", request.Scene);
        Assert.Null(SoundRequest.Parse("""{"action":"led.start","target":"led:tor"}"""));
        Assert.Null(SoundRequest.Parse("kein json"));
    }
}
