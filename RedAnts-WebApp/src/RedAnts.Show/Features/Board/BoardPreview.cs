using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Board;

public sealed record PreviewTitle(string Kind, string Side, string Name, string Number = "", string Event = "", string Info = "");

public sealed record PreviewTeams(string Home, string Away, string HomeShort, string AwayShort)
{
    public static readonly PreviewTeams Sample = new("Red Ants", "Gegner", "RAW", "GEG");

    public string Of(string side) => side == "away" ? Away : Home;

    public static string OwnSide(Game game) => Own(game.Away) && !Own(game.Home) ? "away" : "home";

    public static GameTeam Team(Game game, string side) =>
        game.Side((side == "away") == (OwnSide(game) == "home") ? "away" : "home");

    public static PreviewTeams From(Game game)
    {
        var own = Team(game, "home");
        var other = Team(game, "away");
        return new(Name(own, Sample.Home), Name(other, Sample.Away), Short(own, Sample.HomeShort), Short(other, Sample.AwayShort));
    }

    static bool Own(GameTeam team) =>
        team.LongName.Contains("Red Ants", StringComparison.OrdinalIgnoreCase) || team.LongName.Contains("Rychenberg", StringComparison.OrdinalIgnoreCase);

    static string Name(GameTeam team, string fallback) => team.LongName is { Length: > 0 } name ? name : fallback;

    static string Short(GameTeam team, string fallback) =>
        team.ShortName is { Length: > 0 } name ? name : team.LongName.Length > 0 ? GameTeam.ShortNameOf(team.LongName) : fallback;
}

public sealed record BoardPreview(
    PreviewTitle? Title = null,
    bool Scoreboard = true,
    string? Led = null,
    string LedText = "",
    int Home = 0,
    int Away = 0,
    PreviewTeams? Names = null)
{
    public static readonly BoardPreview Start = new();

    public static readonly IReadOnlyList<string> Penalties = ["2'", "2+2'", "5'", "10'"];

    public PreviewTeams Teams => Names ?? PreviewTeams.Sample;

    public static Player SamplePlayer(string side) =>
        side == "away" ? new Player(9, "Spielerin Gegner") : new Player(17, "Spielerin Red Ants");

    public static bool Picks(ButtonAction action) =>
        action.Kind == KeyKinds.Special && SpecialKey.ChoosesPlayer(action.Special);

    public static string SideOf(ButtonAction action) =>
        action.Special is { } special && special.EndsWith("-gast", StringComparison.Ordinal) ? "away" : "home";

    public BoardPreview Press(ButtonAction action, BoardFunctions functions)
    {
        var next = Run(Phases(action)[0].Steps, functions);
        if (action.Kind == KeyKinds.Special && action.Special is { } special && !Picks(action))
            next = next.Special(special, functions);
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

    public BoardPreview Picked(ButtonAction action, Player player, BoardFunctions functions, string penalty = "2'")
    {
        var side = SideOf(action);
        var number = player.Number > 0 ? player.Number.ToString() : "";
        var team = Teams.Of(side);
        var live = functions.Livestream && action.Livestream;
        var led = functions.Led && action.Led;
        var own = side == "home";
        var shown = $"{number}|{player.Name}";
        switch (action.Special)
        {
            case SpecialKey.GoalHome or SpecialKey.GoalAway:
                var scored = side == "away" ? this with { Away = Away + 1 } : this with { Home = Home + 1 };
                if (live) scored = scored with { Title = new PreviewTitle("player_event", side, player.Name, number, "TOR", team) };
                return led && own ? scored.WithLed("tor", shown) : scored;
            case SpecialKey.PenaltyHome or SpecialKey.PenaltyAway:
                return live ? this with { Title = new PreviewTitle("penalty", side, player.Name, number, penalty, $"Strafe · {team}") } : this;
            case SpecialKey.BestPlayerHome or SpecialKey.BestPlayerAway:
                var best = live ? this with { Title = new PreviewTitle("bestplayer", side, player.Name, number, "MVP", $"Best Player · {team}") } : this;
                return led && own ? best.WithLed("spielerin", shown) : best;
            case SpecialKey.PlayerHome or SpecialKey.PlayerAway:
                return led && own ? WithLed("spielerin", shown) : this;
            case SpecialKey.Starting6Home or SpecialKey.Starting6Away:
                var six = live ? this with { Title = new PreviewTitle("starting6", side, player.Name, number, "", $"Starting Six · {team}") } : this;
                if (!led) return six;
                return own ? six.WithLed("einlauf", shown) : six.WithLed("gast", Teams.Away);
            default:
                return this;
        }
    }

    BoardPreview Step(KeyStep step, BoardFunctions functions)
    {
        switch (step.Action)
        {
            case KeyActions.VmixShow when functions.Livestream:
                var kind = KindOf(step.Target);
                if (IsScoreboard(kind)) return this with { Scoreboard = true };
                var side = step.Side is { Length: > 0 } given ? given : SideOfTarget(step.Target);
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

    BoardPreview Special(string special, BoardFunctions functions) => special switch
    {
        SpecialKey.GuestLogo when functions.Led => WithLed("gast", Teams.Away),
        SpecialKey.OverlayToggle when functions.Livestream => this with { Title = null },
        SpecialKey.ClockToggle when functions.Livestream => this with { Scoreboard = !Scoreboard },
        _ => this,
    };

    BoardPreview WithLed(string name, string text) => this with { Led = name, LedText = text };

    public PreviewTitle TitleOf(string kind, string side, string text = "")
    {
        var team = side.Length > 0 ? Teams.Of(side) : "";
        var player = SamplePlayer(side);
        var number = player.Number.ToString();
        return kind switch
        {
            "player_event" => new PreviewTitle(kind, side, player.Name, number, "TOR", team),
            "penalty" => new PreviewTitle(kind, side, player.Name, number, "2'", $"Strafe · {team}"),
            "bestplayer" => new PreviewTitle(kind, side, player.Name, number, "MVP", $"Best Player · {team}"),
            "name" or "starting6" => new PreviewTitle(kind, side, player.Name, number, "", team),
            "timeout" => new PreviewTitle(kind, side, "Timeout", Info: team),
            "coach" => new PreviewTitle(kind, side, "Coach", Info: team),
            "referee" => new PreviewTitle(kind, side, "Schiedsrichter", Info: "Schiedsrichterpaar"),
            "commentary" => new PreviewTitle(kind, side, "Kommentar", Info: text),
            "result" => new PreviewTitle(kind, side, $"{Teams.Home} – {Teams.Away}", Info: "Resultat"),
            "opener" => new PreviewTitle(kind, side, $"{Teams.Home} – {Teams.Away}", Info: "Heute live"),
            "lineup_home" => new PreviewTitle(kind, "home", Teams.Home, Info: "Aufstellung"),
            "lineup_away" => new PreviewTitle(kind, "away", Teams.Away, Info: "Aufstellung"),
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

    static string SideOfTarget(string? target)
    {
        var text = target ?? "";
        if (text.EndsWith("_away", StringComparison.OrdinalIgnoreCase) || text.EndsWith("-gast", StringComparison.OrdinalIgnoreCase)) return "away";
        if (text.EndsWith("_home", StringComparison.OrdinalIgnoreCase) || text.EndsWith("-heim", StringComparison.OrdinalIgnoreCase)) return "home";
        return "";
    }
}
