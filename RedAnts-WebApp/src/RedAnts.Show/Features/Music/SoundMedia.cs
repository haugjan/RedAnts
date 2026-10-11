using System.Globalization;
using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Music;

public static class SoundMedia
{
    public const string Folder = "/dj/sound/";

    public static string File(string reference) => Folder + reference.Trim();

    public static string Url(SoundTrack track) => Url(track.Ref, track.StartSec, track.DurationSec);

    public static string Url(string reference, double startSec, double? durationSec)
    {
        var file = File(reference);
        if (startSec <= 0 && durationSec is null) return file;
        var start = startSec.ToString("0.###", CultureInfo.InvariantCulture);
        var end = durationSec is { } length ? "," + (startSec + length).ToString("0.###", CultureInfo.InvariantCulture) : "";
        return $"{file}#t={start}{end}";
    }
}
