using NPoco;
using RedAnts.Domain;
using RedAnts.Game.Domain;
using RedAnts.Game.Infrastructure;

namespace RedAnts.Game.Features.Admin.Infrastructure;

public class DemoCandidateRow
{
    public int ExternalId { get; set; }
    public int Position { get; set; }
    public int MarketValue { get; set; }
}

public sealed class DemoSquads(GameDatabase database) : IDemoSquads
{
    public const string TokenPrefix = "demo-";

    private static readonly string[] Names =
    [
        "Demo Alpha", "Demo Bravo", "Demo Charlie", "Demo Delta", "Demo Echo", "Demo Foxtrot",
        "Demo Golf", "Demo Hotel", "Demo India", "Demo Juliett", "Demo Kilo", "Demo Lima",
    ];

    public Task<int> CountAsync() => database.RunAsync(db => db.ExecuteScalarAsync<int>(
        "SELECT COUNT(1) FROM [game].[Squads] WHERE [ManagerToken] LIKE @0", TokenPrefix + "%"));

    public Task<int> DeleteAsync() => database.RunAsync(async db =>
    {
        await db.ExecuteAsync("DELETE FROM [game].[SquadSlots] WHERE [ManagerToken] LIKE @0", TokenPrefix + "%");
        return await db.ExecuteAsync("DELETE FROM [game].[Squads] WHERE [ManagerToken] LIKE @0", TokenPrefix + "%");
    });

    public Task<int> CreateAsync(int count, int budget) => database.RunAsync(async db =>
    {
        var candidates = await db.FetchAsync<DemoCandidateRow>(
            "SELECT [ExternalId],[Position],[MarketValue] FROM [game].[Players]");
        if (candidates.Count == 0) throw new DomainException("Ohne Spielerinnen lässt sich kein Demokader bauen.");

        var existing = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [game].[Squads] WHERE [ManagerToken] LIKE @0", TokenPrefix + "%");

        var random = new Random();
        var created = 0;

        for (var index = 0; index < count; index++)
        {
            var token = TokenPrefix + Guid.NewGuid().ToString("N")[..16];
            var name = Names[(existing + index) % Names.Length];
            var now = SwissTime.Timestamp;
            var picks = Pick(candidates, budget, random);
            if (picks.Count == 0) continue;

            await db.InsertAsync(new SquadRecord
            {
                ManagerToken = token,
                Name = name,
                CreatedAt = now,
                UpdatedAt = now,
            });

            foreach (var (slot, candidate) in picks)
                await db.InsertAsync(new SquadSlotRecord
                {
                    ManagerToken = token,
                    Slot = slot,
                    PlayerId = candidate.ExternalId,
                    MarketValue = candidate.MarketValue,
                });

            created++;
        }

        return created;
    });

    private static List<(int Slot, DemoCandidateRow Player)> Pick(
        List<DemoCandidateRow> candidates, int budget, Random random)
    {
        var byRole = candidates
            .GroupBy(candidate => (PlayerPosition)candidate.Position)
            .ToDictionary(group => group.Key, group => group.OrderBy(_ => random.Next()).ToList());

        var picks = new List<(int Slot, DemoCandidateRow Player)>();
        var used = new HashSet<int>();
        var spent = 0;

        for (var slot = 0; slot < SquadLayout.Slots; slot++)
        {
            var role = SquadLayout.RoleOf(slot);
            if (!byRole.TryGetValue(role, out var pool)) continue;

            var remaining = SquadLayout.Slots - slot - 1;
            var choice = pool.FirstOrDefault(candidate =>
                !used.Contains(candidate.ExternalId)
                && spent + candidate.MarketValue + remaining * GamePlayer.MinMarketValue <= budget);

            if (choice is null) continue;

            used.Add(choice.ExternalId);
            spent += choice.MarketValue;
            picks.Add((slot, choice));
        }

        return picks;
    }
}
