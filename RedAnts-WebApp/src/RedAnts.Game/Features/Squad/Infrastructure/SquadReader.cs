using NPoco;
using RedAnts.Game.Domain;
using RedAnts.Game.Features.Players.Infrastructure;
using RedAnts.Game.Features.Shared;
using RedAnts.Game.Infrastructure;

namespace RedAnts.Game.Features.Squad.Infrastructure;

public sealed class SquadReader(GameDatabase database) : ISquadReader
{
    public async Task<SquadView> GetViewAsync(string managerToken, int budget)
    {
        var (name, slots, cards) = await database.RunAsync<(string Name, List<SquadSlotRecord> Slots, List<GamePlayerCardRow> Cards)>(async db =>
        {
            var header = await db.FirstOrDefaultAsync<SquadRecord>(
                "SELECT * FROM [game].[Squads] WHERE [ManagerToken] = @0", managerToken);
            var rows = await db.FetchAsync<SquadSlotRecord>(
                "SELECT * FROM [game].[SquadSlots] WHERE [ManagerToken] = @0", managerToken);
            var players = new List<GamePlayerCardRow>();
            if (rows.Count > 0)
                players = await db.FetchAsync<GamePlayerCardRow>(
                    $"SELECT {PlayerCardMapping.CardColumns} FROM [game].[Players] WHERE [ExternalId] IN (@ids)",
                    new { ids = rows.Select(r => r.PlayerId).Distinct().ToArray() });
            return (header?.Name ?? "Mein Kader", rows, players);
        });

        var byId = cards.ToDictionary(card => card.ExternalId, card => card.ToCard());
        var placed = new Dictionary<int, PlayerCardView>();
        var spent = 0;

        foreach (var row in slots)
        {
            if (!SquadLayout.IsSlot(row.Slot)) continue;
            spent += row.MarketValue;
            if (byId.TryGetValue(row.PlayerId, out var card)) placed[row.Slot] = card;
        }

        var lines = new List<SquadLineView>();
        for (var line = 0; line <= SquadLayout.Lines; line++)
        {
            var slotViews = SquadLayout.SlotsOfLine(line)
                .Select(slot => new SquadSlotView(
                    slot,
                    line,
                    SquadLayout.RoleOf(slot),
                    SquadLayout.LabelOf(slot),
                    placed.GetValueOrDefault(slot)))
                .ToList();
            lines.Add(new SquadLineView(line, line == 0 ? "Goalies" : $"{line}. Linie", slotViews));
        }

        return new SquadView(name, budget, spent, placed.Count, lines);
    }
}
