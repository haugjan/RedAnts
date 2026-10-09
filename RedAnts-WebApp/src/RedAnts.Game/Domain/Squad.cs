namespace RedAnts.Game.Domain;

public readonly record struct SquadPick(int PlayerId, int MarketValue);

public sealed class Squad
{
    public const int DefaultBudget = 1600;
    public const int NameMaxLength = 40;

    private readonly SquadPick?[] _slots;

    private Squad(string managerToken, string name, DateTimeOffset createdAt, SquadPick?[] slots)
    {
        ManagerToken = managerToken;
        Name = name;
        CreatedAt = createdAt;
        _slots = slots;
    }

    public string ManagerToken { get; }
    public string Name { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public IReadOnlyList<SquadPick?> Slots => _slots;
    public int Spent => _slots.Where(s => s is not null).Sum(s => s!.Value.MarketValue);
    public int Placed => _slots.Count(s => s is not null);
    public bool IsComplete => Placed == SquadLayout.Slots;

    public static Squad Create(string managerToken, DateTimeOffset createdAt) =>
        Restore(managerToken, "Mein Kader", createdAt, []);

    public static Squad Restore(
        string managerToken,
        string name,
        DateTimeOffset createdAt,
        IEnumerable<(int Slot, SquadPick Pick)> picks)
    {
        if (string.IsNullOrWhiteSpace(managerToken)) throw new ValidationException(nameof(managerToken), "Kein Managerkennzeichen.");
        var slots = new SquadPick?[SquadLayout.Slots];
        foreach (var (slot, pick) in picks)
            if (SquadLayout.IsSlot(slot)) slots[slot] = pick;
        return new Squad(managerToken, Clean(name), createdAt, slots);
    }

    public void Rename(string name) => Name = Clean(name);

    public void Place(int slot, PlayerPosition position, SquadPick pick, int budget)
    {
        if (!SquadLayout.IsSlot(slot)) throw new DomainException($"Platz {slot} gibt es im Kader nicht.");

        var role = SquadLayout.RoleOf(slot);
        if (role != position)
            throw new DomainException($"Auf {SquadLayout.LabelOf(slot)} passt nur {role.Label()}, nicht {position.Label()}.");

        for (var i = 0; i < _slots.Length; i++)
            if (i != slot && _slots[i]?.PlayerId == pick.PlayerId)
                throw new DomainException("Diese Spielerin steht bereits im Kader.");

        var after = Spent - (_slots[slot]?.MarketValue ?? 0) + pick.MarketValue;
        if (after > budget)
            throw new DomainException($"Das Budget reicht nicht: {after} von {budget} FB.");

        _slots[slot] = pick;
    }

    public void Clear(int slot)
    {
        if (!SquadLayout.IsSlot(slot)) throw new DomainException($"Platz {slot} gibt es im Kader nicht.");
        _slots[slot] = null;
    }

    private static string Clean(string? name)
    {
        var cleaned = (name ?? "").Trim();
        if (cleaned.Length == 0) cleaned = "Mein Kader";
        return cleaned.Length > NameMaxLength ? cleaned[..NameMaxLength] : cleaned;
    }
}
