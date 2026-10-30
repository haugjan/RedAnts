using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent.Keys;

public sealed record KeyContext(
    string Side = "",
    Player? Player = null,
    string Penalty = "",
    string Text = "",
    string Event = "",
    string Score = "")
{
    public static readonly KeyContext None = new();
}

public static class Templates
{
    public static string Expand(string template, KeyContext ctx, Game game, LiveGame live)
    {
        if (!template.Contains('{')) return template;
        var team = game.Side(ctx.Side);
        var player = ctx.Player;
        var score = ctx.Score.Length > 0 ? ctx.Score : $"{live.HomeScore}:{live.AwayScore}";
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["player.number"] = player?.Display ?? "",
            ["player.number00"] = Padded(player?.Display),
            ["player.name"] = player?.Name ?? "",
            ["player.position"] = player?.Position ?? "",
            ["player.sponsor"] = player?.Sponsor ?? "",
            ["player.photo"] = player?.Photo ?? "",
            ["team.short"] = team.ShortName,
            ["team.long"] = team.LongName,
            ["team.coach"] = team.Coach,
            ["team.logo"] = team.LogoUrl ?? "",
            ["team.color"] = team.Color,
            ["team.starting6"] = string.Join(", ", team.Starting6Players.Select(p => $"{p.Display} {p.Name}")),
            ["home.short"] = game.Home.ShortName,
            ["home.long"] = game.Home.LongName,
            ["home.coach"] = game.Home.Coach,
            ["home.color"] = game.Home.Color,
            ["away.short"] = game.Away.ShortName,
            ["away.long"] = game.Away.LongName,
            ["away.coach"] = game.Away.Coach,
            ["away.color"] = game.Away.Color,
            ["score"] = score,
            ["score.home"] = Half(score, 0, live.HomeScore),
            ["score.away"] = Half(score, 1, live.AwayScore),
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
            ["referees"] = Both(game.Referee1, game.Referee2),
            ["commentator1"] = game.Commentator1,
            ["commentator2"] = game.Commentator2,
            ["commentators"] = Both(game.Commentator1, game.Commentator2),
            ["topscorer.number"] = team.TopScorerPlayer?.Display ?? "",
            ["topscorer.name"] = team.TopScorerPlayer?.Name ?? "",
        };
        AddRoster(values, "starting6", team.Starting6Players.ToList());
        AddRoster(values, "lineup", team.Players.ToList());
        var result = template;
        foreach (var (key, value) in values)
            result = result.Replace("{" + key + "}", value, StringComparison.OrdinalIgnoreCase);
        return result;
    }

    private static string Padded(string? number)
    {
        var digits = new string((number ?? "").Where(char.IsAsciiDigit).ToArray());
        return digits.Length is > 0 and < 2 ? digits.PadLeft(2, '0') : digits;
    }

    private static string Half(string score, int index, string fallback)
    {
        var parts = score.Split(':');
        return parts.Length > index ? parts[index].Trim() : fallback;
    }

    private static string Both(string first, string second) =>
        string.Join(" / ", new[] { first, second }.Select(name => name.Trim()).Where(name => name.Length > 0));

    private static void AddRoster(Dictionary<string, string> values, string prefix, IReadOnlyList<Player> players)
    {
        for (var position = 1; position <= players.Count; position++)
        {
            var player = players[position - 1];
            values[$"{prefix}.{position}.number"] = player.Display;
            values[$"{prefix}.{position}.name"] = player.Name;
        }
    }
}
