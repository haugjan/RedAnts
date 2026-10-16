using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedAnts.Show.Agent.Cloud;

public static class PayloadJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static readonly JsonSerializerOptions Pretty = new(Options) { WriteIndented = true };
}

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

public sealed record AutomationRule(string StateId, string Value, string SceneId);

public static class KeyKinds
{
    public const string Empty = "leer";
    public const string Music = "musik";
    public const string Steps = "befehle";
    public const string Special = "spezial";
    public const string Page = "seite";
    public const string Live = "anzeige";
}

public static class KeyActions
{
    public const string MusicPlay = "musik.start";
    public const string MusicStop = "musik.stopp";
    public const string MusicFade = "musik.fade";
    public const string VmixShow = "vmix.ein";
    public const string VmixHide = "vmix.aus";
    public const string LedStart = "led.start";
    public const string LedStop = "led.stopp";
    public const string Wait = "warten";
}

public static class SpecialKeys
{
    public const string GoalHome = "tor-heim";
    public const string GoalAway = "tor-gast";
    public const string PenaltyHome = "strafe-heim";
    public const string PenaltyAway = "strafe-gast";
    public const string BestPlayerHome = "bestplayer-heim";
    public const string BestPlayerAway = "bestplayer-gast";
    public const string PlayerHome = "spieler-heim";
    public const string PlayerAway = "spieler-gast";
    public const string Starting6Home = "starting6-heim";
    public const string Starting6Away = "starting6-gast";
    public const string OverlayToggle = "einblender";
    public const string ClockToggle = "uhr";
    public const string MusicToggle = "musik-an-aus";
    public const string AutoBreakMusic = "auto-pausenmusik";
    public const string MusicStop = "musik-stopp";
    public const string MusicFade = "musik-fade";
    public const string MusicNext = "musik-vor";
    public const string MusicPrevious = "musik-zurueck";
    public const string MusicPause = "musik-pause";
    public const string Spectators = "zuschauer";

    public static string Side(string special) =>
        special.EndsWith("-heim", StringComparison.Ordinal) ? "home"
        : special.EndsWith("-gast", StringComparison.Ordinal) ? "away"
        : "";
}

public sealed record KeyStep(string Action, string? Target = null, int? Ms = null, string? Event = null, string Side = "", string Text = "");

public sealed record DeckKey(
    int Number,
    string Kind,
    string Label = "",
    string Color = "#2b3038",
    string? Icon = null,
    string? MusicId = null,
    string? Special = null,
    string? PageId = null,
    string? Live = null,
    IReadOnlyList<KeyStep>? Steps = null,
    bool HomeAfter = false)
{
    public IReadOnlyList<KeyStep> Sequence => Steps ?? [];

    public bool IsEmpty => Kind == KeyKinds.Empty;
}

public sealed record DeckPage(string Id, string Name, string? ParentId, IReadOnlyList<DeckKey> Keys)
{
    public const string HomeId = "start";

    public bool IsHome => Id == HomeId;

    public DeckKey KeyAt(int number) => Keys.FirstOrDefault(k => k.Number == number) ?? new DeckKey(number, KeyKinds.Empty);
}

public sealed record DeckLayout(
    string Id,
    string Name,
    int Columns,
    int Rows,
    IReadOnlyList<DeckPage> Pages,
    int PanicKey = 0,
    int BackKey = 0)
{
    public int Count => Columns * Rows;

    public int Panic => PanicKey > 0 ? PanicKey : Count;

    public int Back => BackKey > 0 ? BackKey : Count - Columns + 1;

    public DeckPage Home => Pages.FirstOrDefault(p => p.IsHome) ?? Pages.FirstOrDefault() ?? new DeckPage(DeckPage.HomeId, "Startseite", null, []);

    public DeckPage? Page(string? id) => id is null ? null : Pages.FirstOrDefault(p => p.Id == id);

    public bool Reserved(int number) => number == Panic || number == Back;
}

public sealed record MusicItem(string Id, string Name, string Folder, int Songs, bool Random, string SongsJson = "[]", string Icon = "", string Color = "", string Subtitle = "")
{
    public string Display => Folder.Length > 0 ? $"{Folder} · {Name}" : Name;
}

public sealed record OverlayTitle(string Kind, string Input, int Channel, IReadOnlyDictionary<string, string> Fields);

public sealed record Hall(
    string Id,
    string Name,
    int TeamId,
    int Season,
    HallDevices Devices,
    IReadOnlyList<LedTemplate> LedTemplates,
    IReadOnlyList<OverlayTitle> OverlayTitles,
    IReadOnlyList<string> Shortcuts,
    string? ActiveGameId = null,
    IReadOnlyList<DeckLayout>? Decks = null,
    HallCapabilities? Offered = null,
    string BreakMusicId = "")
{
    public static readonly Hall Offline = new("", "Keine Halle", 0, 0, new HallDevices(), [], [], []);

    public HallCapabilities Offers => Offered ?? HallCapabilities.Default;

    public DeckLayout? DeckOf(string id) => Decks?.FirstOrDefault(d => d.Id == id);

    public string? LedCommand(string eventName) =>
        LedTemplates.FirstOrDefault(t => t.Event.Equals(eventName, StringComparison.OrdinalIgnoreCase))?.Command;

    public OverlayTitle? Title(string kind) =>
        OverlayTitles.FirstOrDefault(t => t.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase));
}

public enum GameSource { Manual, SwissUnihockey, File }

public sealed record Player(int Number, string Name, string Position = "", string? Photo = null, string? Sponsor = null)
{
    public string Display => Number.ToString();
}

public sealed record GameTeam(
    int? TeamId,
    string LongName,
    string ShortName,
    string Color,
    string? LogoUrl,
    string Coach,
    IReadOnlyList<Player> Players,
    IReadOnlyList<int> Starting6,
    IReadOnlyList<IReadOnlyList<int>> Lines,
    IReadOnlyList<int> Goalies,
    int? TopScorer = null)
{
    public static readonly GameTeam Empty = new(null, "", "", "", null, "", [], [], [], []);

    public Player? TopScorerPlayer => TopScorer is { } number ? Find(number) : null;

    public Player? Find(int number) => Players.FirstOrDefault(p => p.Number == number);

    public IReadOnlyList<Player> Starting6Players =>
        Starting6.Select(Find).OfType<Player>().ToList();

    public bool HasStarting6 => Starting6.Count == 6 && Starting6.All(n => Find(n) is not null);
}

public sealed record Game(
    string Id,
    GameSource Source,
    string Title,
    string League,
    DateTime? Date,
    string Location,
    GameTeam Home,
    GameTeam Away,
    string Referee1,
    string Referee2,
    string Commentator1,
    string Commentator2,
    DateTimeOffset LoadedAt)
{
    public static readonly Game None = new("", GameSource.Manual, "", "", null, "", GameTeam.Empty, GameTeam.Empty, "", "", "", "", DateTimeOffset.MinValue);

    public GameTeam Side(string side) =>
        side.Equals("away", StringComparison.OrdinalIgnoreCase) ? Away : Home;
}

public sealed record AgentPayload(
    Hall Hall,
    Game? Game,
    DateTimeOffset SentAt,
    IReadOnlyList<MusicItem>? Music = null);
