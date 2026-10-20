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
    string ClockSource = "",
    string ImageFolder = "",
    int PeriodMinutes = 20);

public sealed record HallCapabilities(
    bool Sound = true,
    bool Clock = true,
    bool Vmix = true,
    bool Led = true,
    bool Light = false)
{
    public static readonly HallCapabilities Default = new();
}

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
    string? ActiveGameId = null,
    IReadOnlyList<Deck>? Decks = null,
    HallCapabilities? Offered = null,
    string BreakMusicId = "",
    IReadOnlyList<string>? DeckIds = null)
{
    public HallCapabilities Offers => Offered ?? HallCapabilities.Default;

    public IReadOnlyList<Deck> DeckLayouts => Decks is { Count: > 0 } decks ? decks : DefaultDecks.All;

    public const int RedAntsWomen = 428665;

    public static Hall Create(string id, string name)
    {
        ValidationException.ThrowIfBlank(id, nameof(Id), "Kennung");
        ValidationException.ThrowIfBlank(name, nameof(Name), "Name");
        var slug = id.Trim().ToLowerInvariant();
        if (!IdPattern().IsMatch(slug))
            throw new ValidationException(nameof(Id), "Kennung: nur Kleinbuchstaben, Ziffern und Bindestrich, 2 bis 40 Zeichen.");
        return new Hall(slug, name.Trim(), NewKey(), RedAntsWomen, SeasonOf(SwissTime.Today), new HallDevices(),
            DefaultLedTemplates, DefaultOverlayTitles, DefaultShortcuts);
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


    public static class TitleInputs
    {
        public const string Clock = "LUPL-SU_scoreboard_clock.gtzip";
        public const string PlayerEvent = "LUPL-SU_team_player_event.gtzip";
        public const string PlayerName = "LUPL-SU_team_player_name.gtzip";
        public const string TeamInfo = "LUPL-SU_team_info.gtzip";
        public const string LeagueInfo = "LUPL-SU_league_info.gtzip";
        public const string Board = "LUPL-SU_board.gtzip";
        public const string StartingSix = "LUPL-SU_team_startingsix.gtzip";
        public const string LineupHome = "LUPL-SU_team_lineup_home.gtzip";
        public const string LineupAway = "LUPL-SU_team_lineup_away.gtzip";
    }

    public static IReadOnlyList<OverlayTitle> DefaultOverlayTitles { get; } = BuildTitles();

    private static IReadOnlyList<OverlayTitle> BuildTitles() =>
    [
        new("scoreboard", TitleInputs.Clock, 4, new Dictionary<string, string>
        {
            ["time"] = "TxtClockTime{0}.Text",
            ["period"] = "TxtClockPeriod.Text",
            ["homeScore"] = "TxtHomeScore.Text",
            ["awayScore"] = "TxtAwayScore.Text",
            ["homeName"] = "TxtHomeName.Text",
            ["awayName"] = "TxtAwayName.Text",
            ["homePen1"] = "TxtHomePen1{0}.Text",
            ["homePen2"] = "TxtHomePen2{0}.Text",
            ["awayPen1"] = "TxtAwayPen1{0}.Text",
            ["awayPen2"] = "TxtAwayPen2{0}.Text",
            ["homePen1Fill"] = "RectHomePen1.Fill.Color",
            ["homePen2Fill"] = "RectHomePen2.Fill.Color",
            ["awayPen1Fill"] = "RectAwayPen1.Fill.Color",
            ["awayPen2Fill"] = "RectAwayPen2.Fill.Color",
            ["homeLogo"] = "ImgHomeTeam.Source",
            ["awayLogo"] = "ImgAwayTeam.Source",
        }),
        new("player_event", TitleInputs.PlayerEvent, 1, new Dictionary<string, string>
        {
            ["TxtTop.Text"] = "TOR",
            ["TxtNumber.Text"] = "{player.number}",
            ["TxtName.Text"] = "{player.name}",
            ["TxtEvent.Text"] = "{score}",
            ["TxtBottom.Text"] = "{period.label} {clock}",
        }),
        new("penalty", TitleInputs.PlayerEvent, 1, new Dictionary<string, string>
        {
            ["TxtTop.Text"] = "STRAFE",
            ["TxtNumber.Text"] = "{player.number}",
            ["TxtName.Text"] = "{player.name}",
            ["TxtEvent.Text"] = "{penalty}",
            ["TxtBottom.Text"] = "{team.long}",
        }),
        new("name", TitleInputs.PlayerName, 1, new Dictionary<string, string>
        {
            ["TxtTop.Text"] = "{team.short}",
            ["TxtNumber.Text"] = "{player.number}",
            ["TxtName.Text"] = "{player.name}",
            ["TxtBottom.Text"] = "{player.position}",
        }),
        new("bestplayer", TitleInputs.PlayerName, 1, new Dictionary<string, string>
        {
            ["TxtTop.Text"] = "BEST PLAYER",
            ["TxtNumber.Text"] = "{player.number}",
            ["TxtName.Text"] = "{player.name}",
            ["TxtBottom.Text"] = "{team.long}",
        }),
        new("coach", TitleInputs.TeamInfo, 1, new Dictionary<string, string>
        {
            ["TxtTop.Text"] = "COACH",
            ["TxtInfo.Text"] = "{team.coach}",
            ["TxtBottom.Text"] = "{team.long}",
        }),
        new("timeout", TitleInputs.TeamInfo, 1, new Dictionary<string, string>
        {
            ["TxtTop.Text"] = "TIMEOUT",
            ["TxtInfo.Text"] = "{team.long}",
            ["TxtBottom.Text"] = "{period.label} {clock}",
        }),
        new("message", TitleInputs.LeagueInfo, 1, new Dictionary<string, string>
        {
            ["TxtInfo.Text"] = "{text}",
        }),
        new("spectators", TitleInputs.LeagueInfo, 1, new Dictionary<string, string>
        {
            ["TxtTop.Text"] = "ZUSCHAUER",
            ["TxtInfo.Text"] = "{text}",
        }),
        new("commentary", TitleInputs.LeagueInfo, 1, new Dictionary<string, string>
        {
            ["TxtInfo.Text"] = "Kommentar: {commentators}",
        }),
        new("referee", TitleInputs.LeagueInfo, 1, new Dictionary<string, string>
        {
            ["TxtInfo.Text"] = "Schiedsrichterinnen: {referees}",
        }),
        new("result", TitleInputs.Board, 2, new Dictionary<string, string>
        {
            ["TxtTop.Text"] = "{game.league}",
            ["TxtMain.Text"] = "{score}",
            ["TxtHomeTeam.Text"] = "{home.long}",
            ["TxtAwayTeam.Text"] = "{away.long}",
            ["TxtBottom.Text"] = "{period.label}",
        }),
        new("highlight", TitleInputs.Board, 2, new Dictionary<string, string>
        {
            ["TxtTop.Text"] = "{game.league}",
            ["TxtMain.Text"] = "HIGHLIGHTS",
            ["TxtHomeTeam.Text"] = "{home.long}",
            ["TxtAwayTeam.Text"] = "{away.long}",
            ["TxtBottom.Text"] = "{score}",
        }),
        new("opener", TitleInputs.Board, 2, new Dictionary<string, string>
        {
            ["TxtTop.Text"] = "{game.league}",
            ["TxtMain.Text"] = "HEUTE",
            ["TxtHomeTeam.Text"] = "{home.long}",
            ["TxtAwayTeam.Text"] = "{away.long}",
            ["TxtBottom.Text"] = "{game.location}",
        }),
        new("starting6", TitleInputs.StartingSix, 1, Lineup("STARTING 6", ["1", "2", "3", "4", "5", "6"], "starting6")),
        new("lineup_home", TitleInputs.LineupHome, 1, Lineup("AUFSTELLUNG", LineupSlots, "lineup")),
        new("lineup_away", TitleInputs.LineupAway, 1, Lineup("AUFSTELLUNG", LineupSlots, "lineup")),
    ];

    private static string[] LineupSlots =>
        ["51", "52", "11", "12", "13", "14", "15", "21", "22", "23", "24", "25", "31", "32", "33", "34", "35", "41", "42", "43", "44", "45"];

    private static Dictionary<string, string> Lineup(string top, IReadOnlyList<string> slots, string source)
    {
        var fields = new Dictionary<string, string>
        {
            ["TxtTop.Text"] = top,
            ["TxtTeam.Text"] = "{team.long}",
            ["TxtFuncLabel.Text"] = "COACH",
            ["TxtFuncName.Text"] = "{team.coach}",
        };
        for (var position = 0; position < slots.Count; position++)
        {
            fields[$"TxtNumberPos{slots[position]}.Text"] = $"{{{source}.{position + 1}.number}}";
            fields[$"TxtNamePos{slots[position]}.Text"] = $"{{{source}.{position + 1}.name}}";
        }
        return fields;
    }

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
