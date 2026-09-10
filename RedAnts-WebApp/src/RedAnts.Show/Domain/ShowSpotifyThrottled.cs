using RedAnts.Domain;

namespace RedAnts.Show.Domain;

public sealed class ShowSpotifyThrottled(DateTimeOffset until)
    : Exception(Describe(until))
{
    public DateTimeOffset Until { get; } = until;

    private static string Describe(DateTimeOffset until)
    {
        var swiss = SwissTime.ToSwiss(until);
        var left = until - SwissTime.Timestamp;
        return left > TimeSpan.FromMinutes(2)
            ? $"Spotify drosselt die App bis {swiss:HH:mm} Uhr. Bis dahin sind keine Abfragen möglich."
            : "Spotify drosselt die App gerade. In ein paar Sekunden nochmals versuchen.";
    }
}
