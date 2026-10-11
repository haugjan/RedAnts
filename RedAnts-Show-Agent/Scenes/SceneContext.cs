using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent.Scenes;

public sealed class SceneContext
{
    public required Scene Scene { get; init; }
    public required string Side { get; init; }
    public Player? Player { get; set; }
    public string Penalty { get; init; } = "";
    public string Text { get; init; } = "";
    public string Event { get; set; } = "";
    public required string ScoreHomeAtStart { get; init; }
    public required string ScoreAwayAtStart { get; init; }

    public static SceneContext From(Scene scene, IReadOnlyDictionary<string, string> args, Game game, LiveGame live)
    {
        var side = Arg(args, "side") is { Length: > 0 } given ? given.ToLowerInvariant() : scene.Side;
        var player = int.TryParse(Arg(args, "player"), out var number) ? game.Side(side).Find(number) : null;
        return new SceneContext
        {
            Scene = scene,
            Side = side,
            Player = player,
            Penalty = Arg(args, "penalty"),
            Text = Arg(args, "text"),
            Event = Arg(args, "event"),
            ScoreHomeAtStart = live.HomeScore,
            ScoreAwayAtStart = live.AwayScore,
        };
    }

    public SceneContext Nested(Scene scene) => new()
    {
        Scene = scene,
        Side = scene.Side.Length > 0 ? scene.Side : Side,
        Player = Player,
        Penalty = Penalty,
        Text = Text,
        Event = Event,
        ScoreHomeAtStart = ScoreHomeAtStart,
        ScoreAwayAtStart = ScoreAwayAtStart,
    };

    static string Arg(IReadOnlyDictionary<string, string> args, string key) =>
        args.TryGetValue(key, out var value) ? value.Trim() : "";
}

public static class Templates
{
    public static string Expand(string template, SceneContext ctx, Game game, LiveGame live)
    {
        if (!template.Contains('{')) return template;
        var team = game.Side(ctx.Side);
        var player = ctx.Player;
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["player.number"] = player?.Display ?? "",
            ["player.name"] = player?.Name ?? "",
            ["player.position"] = player?.Position ?? "",
            ["player.sponsor"] = player?.Sponsor ?? "",
            ["player.photo"] = player?.Photo ?? "",
            ["team.short"] = team.ShortName,
            ["team.long"] = team.LongName,
            ["team.coach"] = team.Coach,
            ["team.logo"] = team.LogoUrl ?? "",
            ["team.starting6"] = string.Join(", ", team.Starting6Players.Select(p => $"{p.Display} {p.Name}")),
            ["home.short"] = game.Home.ShortName,
            ["home.long"] = game.Home.LongName,
            ["home.coach"] = game.Home.Coach,
            ["away.short"] = game.Away.ShortName,
            ["away.long"] = game.Away.LongName,
            ["away.coach"] = game.Away.Coach,
            ["score"] = $"{live.HomeScore}:{live.AwayScore}",
            ["score.home"] = live.HomeScore,
            ["score.away"] = live.AwayScore,
            ["clock"] = live.Time,
            ["period"] = live.Period,
            ["period.label"] = live.PeriodLabel,
            ["penalty"] = ctx.Penalty,
            ["text"] = ctx.Text,
            ["event"] = ctx.Event,
            ["side"] = ctx.Side,
            ["game.title"] = game.Title,
            ["game.league"] = game.League,
            ["game.location"] = game.Location,
            ["referee1"] = game.Referee1,
            ["referee2"] = game.Referee2,
            ["commentator1"] = game.Commentator1,
            ["commentator2"] = game.Commentator2,
        };
        AddRoster(values, "starting6", team.Starting6Players.ToList());
        AddRoster(values, "lineup", team.Players.ToList());
        var result = template;
        foreach (var (key, value) in values)
            result = result.Replace("{" + key + "}", value, StringComparison.OrdinalIgnoreCase);
        return result;
    }

    static void AddRoster(Dictionary<string, string> values, string prefix, IReadOnlyList<Player> players)
    {
        for (var position = 1; position <= players.Count; position++)
        {
            var player = players[position - 1];
            values[$"{prefix}.{position}.number"] = player.Display;
            values[$"{prefix}.{position}.name"] = player.Name;
        }
    }
}
