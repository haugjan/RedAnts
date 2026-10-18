using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Music;

public sealed record MusicEntry(string Id, string Name, IReadOnlyList<DJSound> Songs, bool Random)
{
    public static MusicEntry Of(DJButton button) =>
        new(button.Id, button.Label, button.EffectiveSongs, button.SongsRandom);

    public DJButton ToButton() =>
        new(Id, Name.Trim(), Songs: Songs, SongsRandom: Random);
}
