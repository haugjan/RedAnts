using NPoco;
using RedAnts.Game.Domain;
using RedAnts.Game.Features.Players.Infrastructure;
using RedAnts.Game.Infrastructure;

namespace RedAnts.Game.Features.Round.Infrastructure;

public sealed class RoundReader(GameDatabase database) : IRoundReader
{
    private const int MinimumGames = 5;

    public async Task<IReadOnlyList<RoundHighlight>> GetHighlightsAsync()
    {
        var rows = await database.RunAsync(db => db.FetchAsync<GamePlayerCardRow>(
            $"""
            SELECT {PlayerCardMapping.CardColumns}
            FROM [game].[Players]
            WHERE [Games] >= @0
            """,
            MinimumGames));

        if (rows.Count == 0) return [];

        var highlights = new List<RoundHighlight>();

        Add(highlights, rows.OrderByDescending(r => r.PeakPoints).FirstOrDefault(),
            "Beste Einzelpartie", r => $"{r.PeakPoints} Pkt", r => $"Spiel {r.PeakGame}");

        Add(highlights, rows.OrderByDescending(r => r.Goals).FirstOrDefault(),
            "Meiste Tore", r => r.Goals.ToString(), _ => "Saisontore");

        Add(highlights, rows.OrderByDescending(r => r.Assists).FirstOrDefault(),
            "Meiste Assists", r => r.Assists.ToString(), _ => "Vorlagen");

        Add(highlights, rows.OrderByDescending(r => r.BestPlayer).FirstOrDefault(),
            "Bestplayer", r => r.BestPlayer.ToString(), _ => "Auszeichnungen");

        Add(highlights, rows.Where(r => r.Position == (int)PlayerPosition.Goalie)
                .OrderBy(r => r.GoalsAgainstPerGame).FirstOrDefault(),
            "Wenigste Gegentore", r => r.GoalsAgainstPerGame.ToString("0.0"), _ => "pro Spiel");

        Add(highlights, rows.OrderByDescending(r => r.RecentAverage).FirstOrDefault(),
            "Form steigend", r => r.RecentAverage.ToString("0.0"), _ => "letzte 5 Spiele");

        return highlights;
    }

    private static void Add(
        List<RoundHighlight> target,
        GamePlayerCardRow? row,
        string kind,
        Func<GamePlayerCardRow, string> value,
        Func<GamePlayerCardRow, string> caption)
    {
        if (row is null) return;
        target.Add(new RoundHighlight(kind, value(row), caption(row), row.ToCard()));
    }
}
