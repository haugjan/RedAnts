using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Board;

public sealed record PreviewTitle(string Kind, string Side, string Name, string Number = "", string Event = "", string Info = "");

public sealed record BoardPreview(
    PreviewTitle? Title = null,
    bool Scoreboard = true,
    string? Led = null,
    string LedText = "",
    int Home = 0,
    int Away = 0)
{
    public const string HomeTeam = "Red Ants";
    public const string AwayTeam = "Gegner";
    public const string HomeShort = "RAW";
    public const string AwayShort = "GEG";

    public static readonly BoardPreview Start = new();

    public static (string Number, string Name) SamplePlayer(string side) =>
        side == "away" ? ("9", "Spielerin Gegner") : ("17", "Spielerin Red Ants");

    public static string TeamOf(string side) => side == "away" ? AwayTeam : HomeTeam;

    public BoardPreview Press(ButtonAction action, BoardFunctions functions)
    {
        var next = Run(Phases(action)[0].Steps, functions);
        if (action.Kind == KeyKinds.Special && action.Special is { } special) next = next.Special(special, action, functions);
        return next;
    }

    public BoardPreview Run(IEnumerable<KeyStep> steps, BoardFunctions functions) =>
        steps.Aggregate(this, (preview, step) => preview.Step(step, functions));

    public static IReadOnlyList<(int Ms, IReadOnlyList<KeyStep> Steps)> Phases(ButtonAction action)
    {
        var phases = new List<(int, IReadOnlyList<KeyStep>)>();
        var wait = 0;
        var current = new List<KeyStep>();
        foreach (var step in action.Sequence)
        {
            if (step.Action != KeyActions.Wait)
            {
                current.Add(step);
                continue;
            }
            phases.Add((wait, current));
            wait = Math.Clamp(step.Ms ?? 0, 0, 120000);
            current = [];
        }
        phases.Add((wait, current));
        return phases;
    }

    BoardPreview Step(KeyStep step, BoardFunctions functions)
    {
        switch (step.Action)
        {
            case KeyActions.VmixShow when functions.Livestream:
                var kind = KindOf(step.Target);
                if (IsScoreboard(kind)) return this with { Scoreboard = true };
                var side = step.Side is { Length: > 0 } given ? given : SideOf(step.Target);
                return this with { Title = TitleOf(kind, side, step.Text) };
            case KeyActions.VmixText when functions.Livestream:
                return this with { Title = new PreviewTitle("message", "", step.Text is { Length: > 0 } text ? text : "Meldung") };
            case KeyActions.VmixHide when functions.Livestream:
                var hidden = KindOf(step.Target);
                if (hidden is "" or "*") return this with { Title = null };
                if (IsScoreboard(hidden)) return this with { Scoreboard = false };
                return Title?.Kind == hidden ? this with { Title = null } : this;
            case KeyActions.LedStart when functions.Led:
                var name = LedName(step.Target);
                return name is "" or "stopp" ? this with { Led = null, LedText = "" } : WithLed(name, "");
            case KeyActions.LedStop when functions.Led:
                return this with { Led = null, LedText = "" };
            default:
                return this;
        }
    }

    BoardPreview Special(string special, ButtonAction action, BoardFunctions functions)
    {
        var side = special.EndsWith("-gast", StringComparison.Ordinal) ? "away" : "home";
        var (number, player) = SamplePlayer(side);
        var team = TeamOf(side);
        var live = functions.Livestream && action.Livestream;
        var led = functions.Led && action.Led;
        var own = side == "home";
        switch (special)
        {
            case SpecialKey.GoalHome or SpecialKey.GoalAway:
                var scored = side == "away" ? this with { Away = Away + 1 } : this with { Home = Home + 1 };
                if (live) scored = scored with { Title = new PreviewTitle("player_event", side, player, number, "TOR", team) };
                return led && own ? scored.WithLed("tor", $"{number}|{player}") : scored;
            case SpecialKey.PenaltyHome or SpecialKey.PenaltyAway:
                return live ? this with { Title = new PreviewTitle("penalty", side, player, number, "2'", $"Strafe · {team}") } : this;
            case SpecialKey.BestPlayerHome or SpecialKey.BestPlayerAway:
                var best = live ? this with { Title = new PreviewTitle("bestplayer", side, player, number, "MVP", $"Best Player · {team}") } : this;
                return led && own ? best.WithLed("spielerin", $"{number}|{player}") : best;
            case SpecialKey.PlayerHome or SpecialKey.PlayerAway:
                return led && own ? WithLed("spielerin", $"{number}|{player}") : this;
            case SpecialKey.Starting6Home or SpecialKey.Starting6Away:
                var six = live ? this with { Title = new PreviewTitle("starting6", side, player, number, "", $"Starting Six · {team}") } : this;
                if (!led) return six;
                return own ? six.WithLed("einlauf", $"{number}|{player}") : six.WithLed("gast", AwayTeam);
            case SpecialKey.GuestLogo:
                return led ? WithLed("gast", AwayTeam) : this;
            case SpecialKey.OverlayToggle:
                return live ? this with { Title = null } : this;
            case SpecialKey.ClockToggle:
                return live ? this with { Scoreboard = !Scoreboard } : this;
            default:
                return this;
        }
    }

    BoardPreview WithLed(string name, string text) => this with { Led = name, LedText = text };

    public static PreviewTitle TitleOf(string kind, string side, string text = "")
    {
        var team = side.Length > 0 ? TeamOf(side) : "";
        var (number, player) = SamplePlayer(side);
        return kind switch
        {
            "player_event" => new PreviewTitle(kind, side, player, number, "TOR", team),
            "penalty" => new PreviewTitle(kind, side, player, number, "2'", $"Strafe · {team}"),
            "bestplayer" => new PreviewTitle(kind, side, player, number, "MVP", $"Best Player · {team}"),
            "name" or "starting6" => new PreviewTitle(kind, side, player, number, "", team),
            "timeout" => new PreviewTitle(kind, side, "Timeout", Info: team),
            "coach" => new PreviewTitle(kind, side, "Coach", Info: team),
            "referee" => new PreviewTitle(kind, side, "Schiedsrichter", Info: "Schiedsrichterpaar"),
            "commentary" => new PreviewTitle(kind, side, "Kommentar", Info: text),
            "result" => new PreviewTitle(kind, side, $"{HomeTeam} – {AwayTeam}", Info: "Resultat"),
            "opener" => new PreviewTitle(kind, side, $"{HomeTeam} – {AwayTeam}", Info: "Heute live"),
            "lineup_home" => new PreviewTitle(kind, "home", HomeTeam, Info: "Aufstellung"),
            "lineup_away" => new PreviewTitle(kind, "away", AwayTeam, Info: "Aufstellung"),
            _ => new PreviewTitle(kind, side, text is { Length: > 0 } ? text : kind),
        };
    }

    public static string TemplateOf(PreviewTitle title) => title.Kind switch
    {
        "player_event" or "penalty" or "bestplayer" => "number2",
        "name" or "starting6" => "number1",
        "result" or "opener" => "card",
        "lineup_home" or "lineup_away" => "lineup",
        _ => title.Info.Length > 0 ? "lines2" : "lines1",
    };

    static bool IsScoreboard(string kind) => kind.Equals("scoreboard", StringComparison.OrdinalIgnoreCase);

    static string KindOf(string? target) =>
        target is { Length: > 0 } text && text.StartsWith("title:", StringComparison.OrdinalIgnoreCase) ? text[6..] : target ?? "";

    static string LedName(string? target) =>
        target is { Length: > 0 } text && text.StartsWith("led:", StringComparison.OrdinalIgnoreCase) ? text[4..] : target ?? "";

    static string SideOf(string? target)
    {
        var text = target ?? "";
        if (text.EndsWith("_away", StringComparison.OrdinalIgnoreCase) || text.EndsWith("-gast", StringComparison.OrdinalIgnoreCase)) return "away";
        if (text.EndsWith("_home", StringComparison.OrdinalIgnoreCase) || text.EndsWith("-heim", StringComparison.OrdinalIgnoreCase)) return "home";
        return "";
    }
}
