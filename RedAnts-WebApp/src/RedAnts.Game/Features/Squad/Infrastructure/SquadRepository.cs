using NPoco;
using RedAnts.Game.Domain;
using RedAnts.Game.Infrastructure;

namespace RedAnts.Game.Features.Squad.Infrastructure;

public sealed class SquadRepository(GameDatabase database) : ISquadRepository
{
    public async Task<Domain.Squad?> GetByManagerTokenAsync(string managerToken)
    {
        var (squad, slots) = await database.RunAsync<(SquadRecord? Header, List<SquadSlotRecord> Slots)>(async db =>
        {
            var header = await db.FirstOrDefaultAsync<SquadRecord>(
                "SELECT * FROM [game].[Squads] WHERE [ManagerToken] = @0", managerToken);
            if (header is null) return (null, []);
            var rows = await db.FetchAsync<SquadSlotRecord>(
                "SELECT * FROM [game].[SquadSlots] WHERE [ManagerToken] = @0 ORDER BY [Slot]", managerToken);
            return (header, rows);
        });

        if (squad is null) return null;

        return Domain.Squad.Restore(
            squad.ManagerToken,
            squad.Name,
            squad.CreatedAt,
            slots.Select(row => (row.Slot, new SquadPick(row.PlayerId, row.MarketValue))));
    }

    public Task SaveAsync(Domain.Squad squad) => database.RunAsync(async db =>
    {
        var now = SwissTime.Timestamp;
        var updated = await db.ExecuteAsync(
            "UPDATE [game].[Squads] SET [Name] = @0, [UpdatedAt] = @1 WHERE [ManagerToken] = @2",
            squad.Name, now, squad.ManagerToken);

        if (updated == 0)
            await db.InsertAsync(new SquadRecord
            {
                ManagerToken = squad.ManagerToken,
                Name = squad.Name,
                CreatedAt = squad.CreatedAt,
                UpdatedAt = now,
            });

        await db.ExecuteAsync("DELETE FROM [game].[SquadSlots] WHERE [ManagerToken] = @0", squad.ManagerToken);

        for (var slot = 0; slot < squad.Slots.Count; slot++)
        {
            if (squad.Slots[slot] is not { } pick) continue;
            await db.InsertAsync(new SquadSlotRecord
            {
                ManagerToken = squad.ManagerToken,
                Slot = slot,
                PlayerId = pick.PlayerId,
                MarketValue = pick.MarketValue,
            });
        }
    });
}
