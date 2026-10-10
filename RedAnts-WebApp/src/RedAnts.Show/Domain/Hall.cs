using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RedAnts.Show.Domain;

public sealed record HallDevices(
    string VmixHost = "",
    int VmixPort = 8099,
    string SednaHost = "",
    int SednaPort = 27182,
    string ClockType = "iCast",
    int ClockPort = 50085,
    string ImageFolder = "");

public sealed record LedTemplate(string Event, string Command);

public sealed record OverlayTitle(string Kind, string Input, int Channel, IReadOnlyDictionary<string, string> Fields);

public sealed partial record Hall(
    string Id,
    string Name,
    string AgentKey,
    int TeamId,
    int Season,
    HallDevices Devices,
    IReadOnlyList<LedTemplate> LedTemplates,
    IReadOnlyList<OverlayTitle> OverlayTitles,
    IReadOnlyList<string> Shortcuts,
    string? ActiveGameId = null)
{
    public const int RedAntsWomen = 428665;

    public static Hall Create(string id, string name)
    {
        ValidationException.ThrowIfBlank(id, nameof(Id), "Kennung");
        ValidationException.ThrowIfBlank(name, nameof(Name), "Name");
        var slug = id.Trim().ToLowerInvariant();
        if (!IdPattern().IsMatch(slug))
            throw new ValidationException(nameof(Id), "Kennung: nur Kleinbuchstaben, Ziffern und Bindestrich, 2 bis 40 Zeichen.");
        return new Hall(slug, name.Trim(), NewKey(), RedAntsWomen, SeasonOf(SwissTime.Today), new HallDevices(),
            DefaultLedTemplates, [], DefaultShortcuts);
    }

    public static int SeasonOf(DateOnly day) => day.Month >= 7 ? day.Year : day.Year - 1;

    public static string NewKey() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    public bool AcceptsKey(string? key) =>
        !string.IsNullOrEmpty(key)
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(AgentKey));

    public Hall Validated()
    {
        ValidationException.ThrowIfBlank(Name, nameof(Name), "Name");
        ValidationException.ThrowIfLongerThan(Name, 60, nameof(Name), "Name");
        if (Season is < 2020 or > 2100) throw new ValidationException(nameof(Season), "Saison: Jahr zwischen 2020 und 2100.");
        if (TeamId <= 0) throw new ValidationException(nameof(TeamId), "Team-ID von swissunihockey ist Pflicht.");
        return this with { Name = Name.Trim() };
    }

    public Hall WithActiveGame(string? gameId) =>
        this with { ActiveGameId = string.IsNullOrWhiteSpace(gameId) ? null : gameId.Trim() };

    public Hall WithNewKey() => this with { AgentKey = NewKey() };

    public static readonly IReadOnlyList<LedTemplate> DefaultLedTemplates =
    [
        new("tor-heim", "start playlist -n \"Tor Heim\" -o -fi 0.5"),
        new("tor-gast", "start playlist -n \"Tor Gast\" -o -fi 0.5"),
        new("spielerin", "start playlist -n \"Spielerin {team.short} {player.number}\" -o -fi 0.5"),
        new("pause", "start playlist -n \"Pause\""),
        new("timeout", "start playlist -n \"Timeout\" -o"),
        new("intermission", "start playlist -n \"Intermission\""),
        new("sponsoren", "start playlist -n \"Sponsoren\""),
        new("stopp", "stop playlist -o -fo 0.5"),
    ];

    public static readonly IReadOnlyList<string> DefaultShortcuts =
    [
        "Entschuldigen Sie die technische Störung! Wir arbeiten mit Hochdruck an der Lösung!",
        "Highlights",
        "Leider heute ohne Kommentar",
        "Highlights 1. Drittel",
        "Highlights 2. Drittel",
        "Highlights 3. Drittel",
    ];

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,39}$")]
    private static partial Regex IdPattern();
}
