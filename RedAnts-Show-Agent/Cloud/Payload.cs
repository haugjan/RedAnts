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
    string ImageFolder = "");

public sealed record LedTemplate(string Event, string Command);

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
    string? ActiveGameId = null)
{
    public static readonly Hall Offline = new("", "Keine Halle", 0, 0, new HallDevices(), [], [], []);

    public string? LedCommand(string eventName) =>
        LedTemplates.FirstOrDefault(t => t.Event.Equals(eventName, StringComparison.OrdinalIgnoreCase))?.Command;

    public OverlayTitle? Title(string kind) =>
        OverlayTitles.FirstOrDefault(t => t.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase));
}

public static class SceneActions
{
    public const string SoundPlay = "sound.play";
    public const string SoundStop = "sound.stop";
    public const string SoundFade = "sound.fade";
    public const string LedStart = "led.start";
    public const string LedStop = "led.stop";
    public const string OverlayShow = "overlay.show";
    public const string OverlayHide = "overlay.hide";
    public const string LightScene = "light.scene";
    public const string DeckContext = "deck.context";
    public const string PlayerPick = "player.pick";
    public const string Wait = "wait";
    public const string WaitScore = "wait.score";
    public const string SceneRun = "scene.run";
}

public sealed record SceneStep(
    string Action,
    string? Target = null,
    int? Ms = null,
    int? TimeoutMs = null,
    bool Overlay = false,
    double? FadeIn = null,
    string? Event = null);

public sealed record Scene(string Id, string Label, string Side, IReadOnlyList<SceneStep> Steps);

public enum GameSource { SwissUnihockey, File, Manual }

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
    IReadOnlyList<int> Goalies)
{
    public static readonly GameTeam Empty = new(null, "", "", "", null, "", [], [], [], []);

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

public sealed record AgentPayload(Hall Hall, IReadOnlyList<Scene> Scenes, Game? Game, DateTimeOffset SentAt);
